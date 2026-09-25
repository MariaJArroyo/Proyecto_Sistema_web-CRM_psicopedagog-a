using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using MenteActiva.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace MenteActiva.Controllers;

// Todo el panel exige sesion de personal interno. Las pantallas de usuarios
// llevan ademas su propia restriccion, mas abajo.
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class AdminController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public AdminController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public IActionResult Agenda() => View();

    #region Clientes

    public IActionResult Clientes()
    {
        using var client = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "clientes";

        var clientes = client
            .GetFromJsonAsync<List<ClienteViewModel>>(url)
            .Result;

        CargarServicios();

        return View(clientes ?? new List<ClienteViewModel>());
    }

    #region registrar cliente
    [HttpGet]
    public IActionResult RegistrarCliente()
    {
        return RedirectToAction("Clientes");
    }

    [HttpPost]
    public IActionResult RegistrarCliente(ClienteRequestViewModel modelo)
    {
        if (!ModelState.IsValid || modelo.IdServicioInteres <= 0)
        {
            CargarServicios();
            return RedirectToAction("Clientes");
        }

        using var client = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "clientes";

        var result = client
            .PostAsJsonAsync(url, modelo)
            .Result;

        if (result.IsSuccessStatusCode)
        {
            return RedirectToAction("Clientes");
        }

        return Content("Error al registrar el cliente.");
    }
    #endregion

    #region cargar servicios
    private void CargarServicios()
    {
        using var client = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "servicios";

        var servicios = client
            .GetFromJsonAsync<List<ServicioViewModel>>(url)
            .Result;

        ViewBag.Servicios = servicios ?? new List<ServicioViewModel>();
    }

    #endregion

    #region desactivar cliente
    // Excepcion al token antifalsificacion: clientes.js llama esta accion por
    // fetch y no manda el token. Sin esta linea dejaria de funcionar. Hay que
    // arreglarlo desde el JS y quitar esta excepcion.
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult DesactivarCliente(int id)
    {
        using var client = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI")
                  + "clientes/"
                  + id;

        var result = client.PutAsync(url, null).Result;

        if (result.IsSuccessStatusCode)
        {
            return Ok();
        }

        return BadRequest();
    }


    #endregion

    #endregion


    #region Estudiantes

    public IActionResult Estudiantes()
    {
        using var client = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI")
                  + "estudiantes";

        var estudiantes = client
            .GetFromJsonAsync<List<EstudianteViewModel>>(url)
            .Result;

        return View(estudiantes ?? new List<EstudianteViewModel>());
    }

    #region Ficha Estudiante

    public IActionResult EstudianteFicha(int id)
    {
        using var client = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI")
                  + "estudiantes/"
                  + id;

        var estudiante = client
            .GetFromJsonAsync<FichaEstudianteViewModel>(url)
            .Result;

        if (estudiante == null)
        {
            return RedirectToAction("Estudiantes");
        }

        return View(estudiante);
    }

    #endregion

    #endregion


    public IActionResult Comunicacion() => View();
    public IActionResult Dashboard() => View();
    public IActionResult Materiales() => View();
    public IActionResult MiPerfil() => View();
    public IActionResult Pagos() => View();
    public IActionResult Planes() => View();
    public IActionResult Reportes() => View();
    public IActionResult Sesiones() => View();

    #region Usuarios

    // La administracion de cuentas internas es solo del Administrador
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Usuarios()
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "usuarios";

        var usuarios = await cliente.GetFromJsonAsync<List<UsuarioViewModel>>(url);

        return View(usuarios ?? new List<UsuarioViewModel>());
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise los datos: el nombre, el correo y el rol son obligatorios.";
            return RedirectToAction(nameof(Usuarios));
        }

        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "usuarios";

        var respuesta = await cliente.PostAsJsonAsync(url, modelo);

        if (respuesta.IsSuccessStatusCode)
        {
            TempData["Mensaje"] = "Usuario creado. Se le envió el enlace para definir su contraseña.";
        }
        else
        {
            var error = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            TempData["Error"] = error is not null && error.TryGetValue("mensaje", out var mensaje)
                ? mensaje
                : "No se pudo crear el usuario.";
        }

        return RedirectToAction(nameof(Usuarios));
    }

    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> UsuariosExternos()
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "usuarios-externos";

        var encargados = await cliente.GetFromJsonAsync<List<UsuarioExternoViewModel>>(url);

        return View(encargados ?? new List<UsuarioExternoViewModel>());
    }

    // HU-M13-1
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AsignarRol(int idUsuario, int idRol)
    {
        if (idRol <= 0)
        {
            TempData["Error"] = "Seleccione un rol válido.";
            return RedirectToAction(nameof(Usuarios));
        }

        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + $"usuarios/{idUsuario}/rol";

        var respuesta = await cliente.PutAsJsonAsync(url, new { idRol });

        if (!respuesta.IsSuccessStatusCode)
        {
            var error = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            TempData["Error"] = error is not null && error.TryGetValue("mensaje", out var mensaje)
                ? mensaje
                : "No se pudo asignar el rol.";

            return RedirectToAction(nameof(Usuarios));
        }

        // Si se cambio el rol propio, la sesion actual sigue llevando el rol
        // viejo. Se cierra para que la persona entre de nuevo con el que le
        // acaban de poner.
        if (idUsuario == ObtenerIdUsuarioActual())
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Mensaje"] = "Cambió su propio rol. Vuelva a iniciar sesión.";

            return RedirectToAction("Login", "Cuenta");
        }

        TempData["Mensaje"] = "El rol quedó asignado.";

        return RedirectToAction(nameof(Usuarios));
    }

    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> InvitarEncargado(int idEncargado)
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "usuarios-externos";

        var respuesta = await cliente.PostAsJsonAsync(url, new { idEncargado });

        if (respuesta.IsSuccessStatusCode)
        {
            TempData["Mensaje"] = "Se envió la invitación al portal.";
        }
        else
        {
            var error = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            TempData["Error"] = error is not null && error.TryGetValue("mensaje", out var mensaje)
                ? mensaje
                : "No se pudo enviar la invitación.";
        }

        return RedirectToAction(nameof(UsuariosExternos));
    }

    #endregion
    
}
