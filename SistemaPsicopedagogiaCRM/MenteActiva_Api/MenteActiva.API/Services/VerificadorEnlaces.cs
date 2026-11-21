using System.Net;
using System.Net.Sockets;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

// Comprueba si un enlace responde antes de abrirlo (HU-M11-5). La url la
// escribio una persona, asi que la peticion sale con estas protecciones:
//
// - La direccion se valida en el momento de conectar (ConnectCallback), no
//   antes: se resuelve el DNS, se revisan todas las IP y se conecta a esas
//   mismas IP. Validar el nombre y dejar que el cliente vuelva a resolver
//   abre la puerta a un DNS que cambia de respuesta entre las dos consultas.
// - Las redirecciones se siguen a mano, y cada salto vuelve a pasar por la
//   misma conexion validada.
// - Sin proxy: con proxy la conexion iria al proxy y el control no serviria.
// - Cinco segundos en total, contando todos los saltos. El cuerpo no se lee.
//
// "No funciona" es: no conecta, se pasa del tiempo, apunta a una direccion
// bloqueada, o responde 404, 410 o 5xx. Un 401/403 cuenta como que funciona:
// el sitio existe y suele ser que no le gustan los robots.
public sealed class VerificadorEnlaces : IVerificadorEnlaces
{
    public const string NombreCliente = "VerificadorEnlaces";

    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(5);
    private const int MaxRedirecciones = 5;

    private readonly IHttpClientFactory _fabrica;
    private readonly ILogger<VerificadorEnlaces> _registro;

    public VerificadorEnlaces(IHttpClientFactory fabrica, ILogger<VerificadorEnlaces> registro)
    {
        _fabrica = fabrica;
        _registro = registro;
    }

    // Se registra en Program.cs como el manejador del cliente con nombre
    public static SocketsHttpHandler CrearManejador() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        UseCookies = false,
        ConnectTimeout = TiempoMaximo,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        ConnectCallback = ConectarSoloAPublicasAsync
    };

    public async Task<VerificacionEnlaceResponse> VerificarAsync(
        string url, CancellationToken cancelacion)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var actual) || !EsWeb(actual))
        {
            return NoFunciona("La dirección del enlace no es válida.");
        }

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancelacion);
        limite.CancelAfter(TiempoMaximo);

        var cliente = _fabrica.CreateClient(NombreCliente);

        try
        {
            for (var salto = 0; salto <= MaxRedirecciones; salto++)
            {
                using var respuesta = await PedirAsync(cliente, actual, limite.Token);
                var codigo = (int)respuesta.StatusCode;

                if (codigo is >= 300 and < 400 && respuesta.Headers.Location is { } destino)
                {
                    actual = destino.IsAbsoluteUri ? destino : new Uri(actual, destino);

                    if (!EsWeb(actual))
                    {
                        return NoFunciona("El enlace redirige a una dirección que no es una página web.");
                    }

                    continue;
                }

                return codigo is 404 or 410 or >= 500
                    ? NoFunciona($"El enlace no está disponible (el sitio respondió {codigo}).")
                    : new VerificacionEnlaceResponse { Disponible = true, Mensaje = "El enlace está disponible." };
            }

            return NoFunciona("El enlace redirige demasiadas veces.");
        }
        catch (OperationCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            return NoFunciona("El enlace no respondió a tiempo.");
        }
        catch (HttpRequestException ex)
        {
            return Clasificar(ex);
        }
    }

    // HEAD primero para no bajar nada; hay servidores que no lo aceptan o lo
    // contestan distinto, asi que si falla se repite con GET sin leer el cuerpo
    private static async Task<HttpResponseMessage> PedirAsync(
        HttpClient cliente, Uri url, CancellationToken cancelacion)
    {
        var respuesta = await cliente.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, url),
            HttpCompletionOption.ResponseHeadersRead, cancelacion);

        if ((int)respuesta.StatusCode < 400)
        {
            return respuesta;
        }

        respuesta.Dispose();

        return await cliente.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, url),
            HttpCompletionOption.ResponseHeadersRead, cancelacion);
    }

    private VerificacionEnlaceResponse Clasificar(HttpRequestException ex)
    {
        for (Exception? interna = ex; interna is not null; interna = interna.InnerException)
        {
            if (interna is DireccionBloqueadaException)
            {
                return NoFunciona("El enlace apunta a una dirección interna y no se puede abrir.");
            }

            if (interna is SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData })
            {
                return NoFunciona("No se encontró el sitio del enlace.");
            }
        }

        _registro.LogInformation(ex, "El enlace no respondio");

        return NoFunciona("No se pudo conectar con el sitio del enlace.");
    }

    private static async ValueTask<Stream> ConectarSoloAPublicasAsync(
        SocketsHttpConnectionContext contexto, CancellationToken cancelacion)
    {
        var servidor = contexto.DnsEndPoint.Host;
        var direcciones = await Dns.GetHostAddressesAsync(servidor, cancelacion);

        // Basta una bloqueada para rechazar: si no, el sistema podria elegir
        // justo esa al conectar
        if (direcciones.Length == 0 || direcciones.Any(DireccionesBloqueadas.EsBloqueada))
        {
            throw new DireccionBloqueadaException(servidor);
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            await socket.ConnectAsync(direcciones, contexto.DnsEndPoint.Port, cancelacion);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool EsWeb(Uri url)
        => url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps;

    private static VerificacionEnlaceResponse NoFunciona(string mensaje)
        => new() { Disponible = false, Mensaje = mensaje };
}
