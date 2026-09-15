using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
