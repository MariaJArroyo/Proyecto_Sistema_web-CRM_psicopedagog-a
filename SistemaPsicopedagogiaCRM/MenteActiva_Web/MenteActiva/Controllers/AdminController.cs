using Microsoft.AspNetCore.Mvc;
using System.Net;
using MenteActiva.Models;

namespace MenteActiva.Controllers;

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
    [HttpPost]
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
    public IActionResult Usuarios() => View();
    public IActionResult UsuariosExternos() => View();
    
}
