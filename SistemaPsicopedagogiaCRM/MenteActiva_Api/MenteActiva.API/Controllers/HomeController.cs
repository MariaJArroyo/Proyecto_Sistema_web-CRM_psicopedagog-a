using Microsoft.AspNetCore.Mvc;
using Dapper;
using MySqlConnector;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
