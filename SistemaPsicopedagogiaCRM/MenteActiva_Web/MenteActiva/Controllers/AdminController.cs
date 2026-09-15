using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Controllers;

public class AdminController : Controller
{
    public IActionResult Agenda() => View();
    public IActionResult Clientes() => View();
    public IActionResult Comunicacion() => View();
    public IActionResult Dashboard() => View();
    public IActionResult EstudianteFicha() => View();
    public IActionResult Estudiantes() => View();
    public IActionResult Materiales() => View();
    public IActionResult MiPerfil() => View();
    public IActionResult Pagos() => View();
    public IActionResult Planes() => View();
    public IActionResult Reportes() => View();
    public IActionResult Sesiones() => View();
    public IActionResult Usuarios() => View();
    public IActionResult UsuariosExternos() => View();
}
