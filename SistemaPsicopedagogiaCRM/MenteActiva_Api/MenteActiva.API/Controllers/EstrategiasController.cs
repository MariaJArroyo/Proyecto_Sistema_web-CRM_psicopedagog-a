using Dapper;
using MenteActiva.API.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Security.Claims;

namespace MenteActiva.API.Controllers;

[ApiController]
[Route("api/estrategias")]
public class EstrategiasController : ControllerBase
{
    private readonly IConfiguration _config;

    public EstrategiasController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost]
    public IActionResult Crear([FromBody] EstrategiaCrearRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensaje = "Revise los datos de la estrategia."
            });
        }

        if (request.IdPlan <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe indicar un plan válido."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Descripcion))
        {
            return BadRequest(new
            {
                mensaje = "La descripción de la estrategia es obligatoria."
            });
        }

        var idUsuario = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(idUsuario, out var idUsuarioAccion))
        {
            return Unauthorized(new
            {
                mensaje = "No se pudo identificar al usuario."
            });
        }

        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection")
        );

        var parametros = new DynamicParameters();

        parametros.Add(
            "p_IdUsuarioAccion",
            idUsuarioAccion
        );

        parametros.Add(
            "p_IdPlan",
            request.IdPlan
        );

        parametros.Add(
            "p_Descripcion",
            request.Descripcion.Trim()
        );

        parametros.Add(
            "p_Orden",
            request.Orden
        );

        try
        {
            var resultado = conexion.QuerySingle<int>(
                "SP_Estrategia_Agregar",
                parametros,
                commandType: CommandType.StoredProcedure
            );

            return Ok(new
            {
                idEstrategia = resultado,
                mensaje = "Estrategia agregada correctamente."
            });
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al agregar la estrategia.",
                detalle = ex.Message
            });
        }
    }


    [HttpGet("plan/{idPlan:int}")]
    public async Task<IActionResult> ListarPorPlan(int idPlan)
    {
        if (idPlan <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe indicar un plan válido."
            });
        }

        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_IdPlan", idPlan);

        var resultado = await conexion.QueryAsync(
            "SP_Estrategia_Listar",
            parametros,
            commandType: CommandType.StoredProcedure);

        return Ok(resultado);
    }


    [HttpDelete("{idEstrategia:int}")]
    public IActionResult Eliminar(int idEstrategia)
    {
        if (idEstrategia <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe indicar una estrategia válida."
            });
        }

        var idUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(idUsuario, out var idUsuarioAccion))
        {
            return Unauthorized(new
            {
                mensaje = "No se pudo identificar al usuario."
            });
        }

        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEstrategia", idEstrategia);

        try
        {
            conexion.Execute(
                "SP_Estrategia_Eliminar",
                parametros,
                commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                mensaje = "Estrategia eliminada correctamente."
            });
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al eliminar la estrategia.",
                detalle = ex.Message
            });
        }
    }


}