using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IConfiguration _config;

    public UsuariosController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        using var conexion = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        // TODO: reemplazar por el nombre real del SP cuando el equipo lo cree
        var resultado = await conexion.QueryAsync<UsuarioResponse>(
            "SP_ConsultarUsuarios",
            commandType: CommandType.StoredProcedure);

        return Ok(resultado);
    }
}
