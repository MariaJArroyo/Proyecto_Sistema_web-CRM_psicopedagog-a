using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using MenteActiva.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace MenteActiva.Controllers;


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

        var clientes = client
            .GetFromJsonAsync<List<ClienteViewModel>>(UrlApi("clientes"))
            .Result;

        CargarCatalogosClientes();

        return View(clientes ?? new List<ClienteViewModel>());
    }

    #region catalogos de clientes
    private void CargarCatalogosClientes()
    {
        using var client = _http.CreateClient();

        ViewBag.Servicios = client
            .GetFromJsonAsync<List<ServicioViewModel>>(UrlApi("servicios")).Result
            ?? new List<ServicioViewModel>();

        ViewBag.EstadosCliente = client
            .GetFromJsonAsync<List<EstadoClienteViewModel>>(UrlApi("clientes/estados")).Result
            ?? new List<EstadoClienteViewModel>();

        ViewBag.Parentescos = client
            .GetFromJsonAsync<List<ParentescoViewModel>>(UrlApi("clientes/parentescos")).Result
            ?? new List<ParentescoViewModel>();

        ViewBag.NivelesEducativos = client
            .GetFromJsonAsync<List<NivelEducativoViewModel>>(UrlApi("clientes/niveles")).Result
            ?? new List<NivelEducativoViewModel>();
    }
    #endregion

    #region registrar cliente
    [HttpGet]
    public IActionResult RegistrarCliente() => RedirectToAction("Clientes");

    [HttpPost]
    public async Task<IActionResult> RegistrarCliente(ClienteRegistroViewModel modelo)
    {
        if (modelo.Estudiantes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Debe agregar al menos un estudiante.");
        }

        if (modelo.IdServicioInteres <= 0)
        {
            ModelState.AddModelError(nameof(modelo.IdServicioInteres), "Debe seleccionar un servicio de interes.");
        }

        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(UrlApi("clientes"), modelo);

        return await ResponderSegunApi(respuesta, "No se pudo registrar el cliente.");
    }
    #endregion

    #region obtener cliente
    [HttpGet]
    public async Task<IActionResult> ObtenerCliente(int id)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.GetAsync(UrlApi("clientes/" + id));

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound(new { mensaje = "El cliente no existe." });
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            return StatusCode((int)respuesta.StatusCode,
                new { mensaje = "No se pudo cargar el cliente." });
        }

        var cliente = await respuesta.Content.ReadFromJsonAsync<ClienteDetalleViewModel>();

        return Json(cliente);
    }
    #endregion

    #region editar cliente
    [HttpPost]
    public async Task<IActionResult> EditarCliente(ClienteEditarViewModel modelo)
    {
        if (modelo.IdServicioInteres <= 0)
        {
            ModelState.AddModelError(nameof(modelo.IdServicioInteres), "Debe seleccionar un servicio de interes.");
        }

        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(UrlApi("clientes/" + modelo.Id), modelo);

        return await ResponderSegunApi(respuesta, "No se pudo editar el cliente.");
    }
    #endregion

    #region desactivar cliente
    
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult DesactivarCliente(int id)
    {
        using var client = _http.CreateClient();

        var result = client.PutAsync(UrlApi("clientes/" + id + "/desactivar"), null).Result;

        if (result.IsSuccessStatusCode)
        {
            return Ok();
        }

        return BadRequest();
    }
    #endregion

    #region estudiantes del cliente
    [HttpPost]
    public async Task<IActionResult> AgregarEstudianteCliente(int idCliente, EstudianteClienteViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            UrlApi($"clientes/{idCliente}/estudiantes"), modelo);

        return await ResponderSegunApi(respuesta, "No se pudo agregar el estudiante.");
    }

    [HttpPost]
    public async Task<IActionResult> EditarEstudianteCliente(int idCliente, int idEstudiante, EstudianteClienteViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"clientes/{idCliente}/estudiantes/{idEstudiante}"), modelo);

        return await ResponderSegunApi(respuesta, "No se pudo editar el estudiante.");
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstadoEstudianteCliente(int idCliente, int idEstudiante, bool activo)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"clientes/{idCliente}/estudiantes/{idEstudiante}/estado"), new { activo });

        return await ResponderSegunApi(respuesta, "No se pudo cambiar el estado del estudiante.");
    }
    #endregion

    #region auxiliares de clientes
    private string UrlApi(string ruta)
        => _config.GetValue<string>("Valores:UrlAPI") + ruta;

    // Junta los mensajes de validacion en un solo texto para mostrarlo en el modal
    private IActionResult ErrorDeModelo()
    {
        var errores = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct();

        return BadRequest(new { mensaje = string.Join(" ", errores) });
    }

    
    private static async Task<IActionResult> ResponderSegunApi(HttpResponseMessage respuesta, string mensajePorDefecto)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return new OkResult();
        }

        var mensaje = mensajePorDefecto;

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
           
        }

        return new ObjectResult(new { mensaje }) { StatusCode = (int)respuesta.StatusCode };
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
