using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Controllers;

public class PortalController : Controller
{
    public IActionResult Citas() => View();
    public IActionResult Inicio() => View();
    public IActionResult Pagos() => View();
    public IActionResult Perfil() => View();
    public IActionResult Progreso() => View();
    public IActionResult Tareas() => View();
}
