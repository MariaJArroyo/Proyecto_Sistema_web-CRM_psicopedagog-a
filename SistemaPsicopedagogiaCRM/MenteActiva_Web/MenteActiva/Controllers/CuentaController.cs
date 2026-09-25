using System.Net;
using System.Security.Claims;
using MenteActiva.Models;
using MenteActiva.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Controllers;

[AllowAnonymous]
public class CuentaController : Controller
{
    private const string RolEncargado = "Encargado";

    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public CuentaController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [HttpGet]
    public IActionResult Login(string? urlRetorno = null)
    {
        ViewData["UrlRetorno"] = urlRetorno;
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? urlRetorno = null)
    {
        ViewData["UrlRetorno"] = urlRetorno;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "autenticacion/login";

        var respuesta = await cliente.PostAsJsonAsync(url, new
        {
            correo = modelo.CorreoElectronico,
            contrasena = modelo.Contrasena
        });

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            var error = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            ModelState.AddModelError(string.Empty,
                error is not null && error.TryGetValue("mensaje", out var mensaje)
                    ? mensaje
                    : "Correo o contraseña incorrectos.");

            return View(modelo);
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty,
                "No se pudo comunicar con el servicio. Intente de nuevo en unos minutos.");

            return View(modelo);
        }

        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionViewModel>();

        if (sesion is null)
        {
            ModelState.AddModelError(string.Empty, "La respuesta del servicio no se pudo leer.");
            return View(modelo);
        }

        await CrearSesionAsync(sesion);

        if (!string.IsNullOrWhiteSpace(urlRetorno) && Url.IsLocalUrl(urlRetorno))
        {
            return Redirect(urlRetorno);
        }

        // El destino lo decide el rol que vino de la base, nunca el tipo de
        // acceso que eligio la persona en la pantalla.
        return sesion.Rol == RolEncargado
            ? RedirectToAction("Inicio", "Portal")
            : RedirectToAction("Dashboard", "Admin");
    }

    [HttpPost]
    public async Task<IActionResult> CerrarSesion()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccesoDenegado() => View();

    [HttpGet]
    public IActionResult RecuperarContrasena() => View(new RecuperarContrasenaViewModel());

    // Responde siempre lo mismo, exista o no la cuenta. Si distinguiera, esta
    // pantalla serviria para averiguar que correos estan registrados.
    [HttpPost]
    public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "autenticacion/solicitar-restablecimiento";

        var respuesta = await cliente.PostAsJsonAsync(url, new { correo = modelo.CorreoElectronico });

        if (!respuesta.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty,
                "No se pudo comunicar con el servicio. Intente de nuevo en unos minutos.");

            return View(modelo);
        }

        ViewData["CorreoEnviadoA"] = modelo.CorreoElectronico;

        return View(modelo);
    }

    // Llega desde el enlace del correo, sea de invitacion o de recuperacion.
    // Hoy los dos hacen lo mismo: definir la contrasena y dejar la cuenta activa.
    [HttpGet]
    public IActionResult DefinirContrasena(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return View("EnlaceInvalido");
        }

        return View(new DefinirContrasenaViewModel { Token = token });
    }

    [HttpPost]
    public async Task<IActionResult> DefinirContrasena(DefinirContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "autenticacion/activar";

        var respuesta = await cliente.PostAsJsonAsync(url, new
        {
            token = modelo.Token,
            contrasenaNueva = modelo.Contrasena
        });

        if (!respuesta.IsSuccessStatusCode)
        {
            var error = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            ModelState.AddModelError(string.Empty,
                error is not null && error.TryGetValue("mensaje", out var mensaje)
                    ? mensaje
                    : "No se pudo guardar la contraseña. Solicite un enlace nuevo.");

            return View(modelo);
        }

        TempData["Mensaje"] = "Su contraseña quedó guardada. Ya puede iniciar sesión.";

        return RedirectToAction(nameof(Login));
    }

    private async Task CrearSesionAsync(SesionViewModel sesion)
    {
        var datos = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sesion.IdUsuario.ToString()),
            new(ClaimTypes.Name, sesion.NombreCompleto),
            new(ClaimTypes.Email, sesion.Correo),
            new(ClaimTypes.Role, sesion.Rol),
            new(ClaimsMenteActiva.TokenApi, sesion.Token)
        };

        if (sesion.IdEncargado.HasValue)
        {
            datos.Add(new Claim(ClaimsMenteActiva.IdEncargado, sesion.IdEncargado.Value.ToString()));
        }

        var identidad = new ClaimsIdentity(datos, CookieAuthenticationDefaults.AuthenticationScheme);

        // La cookie vence junto con el token que lleva dentro. Si durara mas,
        // quedaria una sesion viva con un token muerto y el API empezaria a
        // responder 401 sin motivo aparente.
        var propiedades = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = sesion.Expira
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            propiedades);
    }
}
