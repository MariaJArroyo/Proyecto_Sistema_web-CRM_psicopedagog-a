using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Controllers;

public class CuentaController : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpGet]
    public IActionResult RecuperarContrasena() => View();
}
