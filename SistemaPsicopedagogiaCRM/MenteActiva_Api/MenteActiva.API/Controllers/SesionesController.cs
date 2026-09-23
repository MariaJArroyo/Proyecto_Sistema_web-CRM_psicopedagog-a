using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using SistemaPsicopedagogia.Api.Models;

namespace SistemaPsicopedagogia.Api.Controllers;

[ApiController]
[Route("api/sesiones")]
public class SesionesController : ControllerBase
{
    private readonly IConfiguration _config;

    public SesionesController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        using var conexion = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        // TODO: reemplazar por el nombre real del SP cuando el equipo lo cree
        var resultado = await conexion.QueryAsync<SesionResponse>(
            "SP_ConsultarSesiones",
            commandType: CommandType.StoredProcedure);

        return Ok(resultado);
    }
}
