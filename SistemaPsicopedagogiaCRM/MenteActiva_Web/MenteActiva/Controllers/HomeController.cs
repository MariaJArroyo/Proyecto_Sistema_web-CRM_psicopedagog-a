using System.Net.Http.Json;
using System.Text.Json;
using MenteActiva.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Controllers;

// El sitio publico es lo unico que se ve sin sesion
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public HomeController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    private string UrlApi(string ruta)
        => _config.GetValue<string>("Valores:UrlAPI") + ruta;

    public async Task<IActionResult> Index()
    {
        // Si el API no responde, la pagina igual carga; solo queda el select sin opciones
        try
        {
            using var client = _http.CreateClient();

            ViewBag.Servicios = await client
                .GetFromJsonAsync<List<ServicioViewModel>>(UrlApi("contacto/servicios"))
                ?? new List<ServicioViewModel>();
        }
        catch
        {
            ViewBag.Servicios = new List<ServicioViewModel>();
        }

        return View();
    }

    // Se llama por fetch desde Validaciones.js. El token antifalsificacion viaja
    // en el FormData (lo valida el filtro global de Program.cs).
    [HttpPost]
    public async Task<IActionResult> EnviarContacto(ContactoViewModel modelo)
    {
        // Bot: se le responde "ok" para que no sepa que fue detectado, y no se guarda nada
        if (!string.IsNullOrWhiteSpace(modelo.SitioWeb))
        {
            return Ok();
        }

        if (!ModelState.IsValid)
        {
            var errores = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct();

            return BadRequest(new { mensaje = string.Join(" ", errores) });
        }

        var solicitud = new
        {
            nombre = modelo.Nombre.Trim(),
            apellido = modelo.Apellido.Trim(),
            telefono = modelo.Telefono.Replace("-", "").Trim(),
            correo = modelo.CorreoElectronico.Trim(),
            idServicioInteres = modelo.IdServicioInteres,
            mensaje = modelo.Mensaje.Trim(),
            // El API solo ve la IP de este servidor; la del visitante se manda aqui
            direccionIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        try
        {
            using var client = _http.CreateClient();

            var respuesta = await client.PostAsJsonAsync(UrlApi("contacto"), solicitud);

            if (respuesta.IsSuccessStatusCode)
            {
                return Ok();
            }

            var mensaje = "No se pudo enviar el mensaje. Intente de nuevo más tarde.";

            try
            {
                var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("mensaje", out var valor) && !string.IsNullOrWhiteSpace(valor.GetString()))
                {
                    mensaje = valor.GetString()!;
                }
            }
            catch
            {
                // Respuesta sin JSON: se queda el mensaje generico
            }

            return StatusCode((int)respuesta.StatusCode, new { mensaje });
        }
        catch
        {
            return StatusCode(503, new { mensaje = "El servicio no está disponible en este momento. Intente más tarde." });
        }
    }
}