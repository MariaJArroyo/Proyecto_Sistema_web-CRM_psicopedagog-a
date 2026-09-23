using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using SistemaPsicopedagogia.Api.Models;

namespace SistemaPsicopedagogia.Api.Controllers;

[ApiController]
[Route("api/materiales")]
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
