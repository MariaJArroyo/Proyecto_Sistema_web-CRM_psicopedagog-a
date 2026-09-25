using Microsoft.AspNetCore.Mvc;
using Dapper;
using MySqlConnector;
using Microsoft.AspNetCore.Authorization;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Sirve para comprobar que el API esta arriba, asi que no pide sesion
[AllowAnonymous]
public class HomeController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }
    


}
