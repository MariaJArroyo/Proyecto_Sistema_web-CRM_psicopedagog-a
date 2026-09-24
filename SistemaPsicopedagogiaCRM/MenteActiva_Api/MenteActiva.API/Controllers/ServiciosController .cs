using Dapper;
using MenteActiva.API.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MenteActiva.Api.Models;
using System.Data;

namespace SistemaPsicopedagogia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiciosController : ControllerBase
{
    private readonly IConfiguration _config;

    public ServiciosController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult ObtenerServicios()
    {
        using var connection = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var servicios = connection.Query<ServicioResponse>(
            "SP_ListarServiciosActivos_CRM",
            commandType: CommandType.StoredProcedure
        ).ToList();

        return Ok(servicios);
    }
}