using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Controllers;

// El sitio publico es lo unico que se ve sin sesion
[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
