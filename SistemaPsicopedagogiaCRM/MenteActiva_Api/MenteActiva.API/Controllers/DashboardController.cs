using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
// Los tres roles internos, como la pantalla del dashboard. Hoy nadie
// llama a este endpoint: la vista es HTML fijo y el procedimiento que
// busca no existe en la base. El rol queda puesto para que el dia que
// se conecte no nazca abierto (RG-115).
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class DashboardController : ControllerBase
{
    private readonly IConfiguration _config;

    public DashboardController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        using var conexion = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        // TODO: reemplazar por el nombre real del SP cuando el equipo lo cree
        var resultado = await conexion.QueryAsync<DashboardResponse>(
            "SP_ConsultarDashboard",
            commandType: CommandType.StoredProcedure);

        return Ok(resultado);
    }
}
