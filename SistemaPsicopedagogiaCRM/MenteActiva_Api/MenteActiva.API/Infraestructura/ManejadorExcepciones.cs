using System.Security.Claims;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace MenteActiva.Api.Infraestructura;

// Un solo lugar que convierte excepciones en respuestas HTTP, para que los
// controladores no repitan try/catch. Responde { mensaje }, el formato que ya
// lee la Web, y deja el fallo escrito en TB_LOG_ERROR.
public sealed class ManejadorExcepciones : IExceptionHandler
{
    private readonly ILogger<ManejadorExcepciones> _registro;

    public ManejadorExcepciones(ILogger<ManejadorExcepciones> registro)
    {
        _registro = registro;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto,
        Exception excepcion,
        CancellationToken cancelacion)
    {
        if (excepcion is ReglaNegocioException regla)
        {
            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            await contexto.Response.WriteAsJsonAsync(new { mensaje = regla.Message }, cancelacion);
            return true;
        }

        // El cliente cerro la peticion: no hay a quien responder ni nada que registrar
        if (excepcion is OperationCanceledException && contexto.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        _registro.LogError(excepcion, "Error no controlado en {Metodo} {Ruta}",
            contexto.Request.Method, contexto.Request.Path);

        // El log a la base va antes de responder, no despues: si se dejara para
        // el final, un cliente que corta la conexion se llevaria el registro.
        // Con su propio try/catch adentro del servicio, asi que no puede tumbar
        // la respuesta. El token es None a proposito: la peticion ya se cayo y
        // cancelarla no debe cancelar el registro del fallo.
        await GuardarEnLogAsync(contexto, excepcion);

        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await contexto.Response.WriteAsJsonAsync(
            new { mensaje = "Ocurrió un error inesperado. Intente de nuevo." }, cancelacion);

        return true;
    }

    // Este manejador es singleton y el servicio del log es por peticion, asi que
    // se pide al contenedor de la peticion y no por constructor. GetService y no
    // GetRequiredService: si faltara el registro, el manejador no es el lugar
    // para reventar.
    private static Task GuardarEnLogAsync(HttpContext contexto, Exception excepcion)
    {
        var servicio = contexto.RequestServices.GetService<IServicioLogError>();

        return servicio is null
            ? Task.CompletedTask
            : servicio.RegistrarAsync(Describir(contexto, excepcion), CancellationToken.None);
    }

    private static ErrorAplicacion Describir(HttpContext contexto, Exception excepcion) => new()
    {
        Nivel = "Error",
        Origen = Origen(contexto),
        Mensaje = excepcion.Message,
        TipoExcepcion = excepcion.GetType().FullName,
        // ToString y no StackTrace: trae tambien las excepciones internas, que
        // es donde suele estar el motivo real
        StackTrace = excepcion.ToString(),
        UrlSolicitada = contexto.Request.Path + contexto.Request.QueryString,
        MetodoHttp = contexto.Request.Method,
        IdUsuario = IdUsuarioONulo(contexto),
        DireccionIp = contexto.Connection.RemoteIpAddress?.ToString(),
        NavegadorAgente = contexto.Request.Headers.UserAgent.ToString()
    };

    // "ClientesController/RegistrarCliente", la forma que ya tenia la tabla.
    //
    // Sale del feature de la excepcion y no de contexto.GetEndpoint(): el
    // middleware de errores borra el endpoint de la peticion antes de llamar
    // aqui, para poder reejecutar la ruta, asi que preguntarselo al contexto
    // devuelve nulo siempre. Se comprobo escribiendo una fila con Origen vacio.
    private static string? Origen(HttpContext contexto)
    {
        var fallo = contexto.Features.Get<IExceptionHandlerFeature>();

        if (fallo is null)
        {
            return null;
        }

        var controlador = fallo.RouteValues?["controller"]?.ToString();
        var accion = fallo.RouteValues?["action"]?.ToString();

        if (!string.IsNullOrWhiteSpace(controlador) && !string.IsNullOrWhiteSpace(accion))
        {
            return $"{controlador}Controller/{accion}";
        }

        return fallo.Endpoint?.DisplayName;
    }

    // Sin ObtenerIdUsuario: esa lanza cuando el token no trae el id, y aqui ya
    // se esta atendiendo un error. Si no se sabe quien era, la columna va nula.
    private static int? IdUsuarioONulo(HttpContext contexto)
    {
        var valor = contexto.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(valor, out var idUsuario) ? idUsuario : null;
    }
}
