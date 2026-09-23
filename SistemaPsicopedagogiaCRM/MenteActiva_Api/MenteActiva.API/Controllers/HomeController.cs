using Microsoft.AspNetCore.Mvc;
using SistemaPsicopedagogia.Api.Data;
using Dapper;
using MySqlConnector;

namespace SistemaPsicopedagogia.Api.Controllers;

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
