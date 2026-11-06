using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/materiales")]
// Los tres roles internos, como la pantalla de materiales. Igual que el
// dashboard, todavia no hay pantalla que lo consuma.
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class MaterialesController : ControllerBase
{
    private readonly IConfiguration _config;

    public MaterialesController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        using var conexion = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        // TODO: reemplazar por el nombre real del SP cuando el equipo lo cree
        var resultado = await conexion.QueryAsync<MaterialResponse>(
            "SP_ConsultarMateriales",
            commandType: CommandType.StoredProcedure);

        return Ok(resultado);
    }
}
