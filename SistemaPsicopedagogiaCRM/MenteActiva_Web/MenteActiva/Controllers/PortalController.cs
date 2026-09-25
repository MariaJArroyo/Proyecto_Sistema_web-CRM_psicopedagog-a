using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Controllers;

// Solo los encargados entran al portal. El personal interno tiene su panel.
[Authorize(Roles = "Encargado")]
public class PortalController : Controller
{
    public IActionResult Citas() => View();
    public IActionResult Inicio() => View();
    public IActionResult Pagos() => View();
    public IActionResult Perfil() => View();
    public IActionResult Progreso() => View();
    public IActionResult Tareas() => View();
}
