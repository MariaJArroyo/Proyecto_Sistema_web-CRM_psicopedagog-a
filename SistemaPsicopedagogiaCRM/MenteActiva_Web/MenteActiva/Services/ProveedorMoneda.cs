using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace MenteActiva.Services;

public interface IProveedorMoneda
{
    Task<string> ObtenerAsync(CancellationToken cancelacion = default);
}

// Trae el codigo ISO de la moneda del API, que lo lee de TB_CONFIGURACION, y lo
// guarda un rato en memoria. Sin el cache cada pantalla con montos agregaria una
// llamada al API solo para enterarse de que la moneda sigue siendo la misma.
//
// El valor lo pide el layout para escribirlo en el body, asi que el JavaScript del
// navegador lo lee de ahi y no hace su propia llamada.
public sealed class ProveedorMoneda : IProveedorMoneda
{
    private const string ClaveCache = "configuracion.moneda";

    // Diez minutos: la moneda no cambia en caliente, y si alguien la cambia en
    // Configuracion basta recargar un rato despues para verlo
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(10);

    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    public ProveedorMoneda(IHttpClientFactory http, IConfiguration config, IMemoryCache cache)
    {
        _http = http;
        _config = config;
        _cache = cache;
    }

    public async Task<string> ObtenerAsync(CancellationToken cancelacion = default)
    {
        if (_cache.TryGetValue(ClaveCache, out string? guardada) && guardada is not null)
        {
            return guardada;
        }

        var moneda = await ConsultarAsync(cancelacion);

        if (moneda is null)
        {
            // No se cachea el fallo: la proxima pantalla vuelve a preguntar. Mostrar
            // los montos en colones es mejor que dejarlos sin simbolo porque el API
            // no contesto.
            return FormatoDinero.MonedaPorDefecto;
        }

        _cache.Set(ClaveCache, moneda, Vigencia);

        return moneda;
    }

    private async Task<string?> ConsultarAsync(CancellationToken cancelacion)
    {
        try
        {
            using var client = _http.CreateClient();

            var url = _config.GetValue<string>("Valores:UrlAPI") + "configuracion/moneda";

            var respuesta = await client.GetFromJsonAsync<JsonElement>(url, cancelacion);

            return respuesta.TryGetProperty("moneda", out var valor)
                ? FormatoDinero.Normalizar(valor.GetString())
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }
}
