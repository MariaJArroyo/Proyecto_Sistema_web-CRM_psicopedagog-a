
using Dapper;
using MenteActiva.API.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Security.Claims;
using MenteActiva.Models;

namespace MenteActiva.API.Controllers;

[ApiController]
[Route("api/actividades")]
public class ActividadesController : ControllerBase
{
    private readonly IConfiguration _config;

    public ActividadesController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost]
    public IActionResult Crear([FromBody] ActividadCrearRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensaje = "Revise los datos de la actividad."
            });
        }

        if (request.IdEstrategia <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe seleccionar una estrategia válida."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(new
            {
                mensaje = "El nombre de la actividad es obligatorio."
            });
        }

        if (request.Nombre.Trim().Length > 150)
        {
            return BadRequest(new
            {
                mensaje = "El nombre no puede superar los 150 caracteres."
            });
        }

        if (request.Descripcion?.Length > 500)
        {
            return BadRequest(new
            {
                mensaje = "La descripción no puede superar los 500 caracteres."
            });
        }

        var idUsuario = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        Console.WriteLine($"ID usuario recibido: {idUsuario}");

        System.Diagnostics.Debug.WriteLine(
    $"ID usuario recibido: {idUsuario ?? "NULL"}"
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

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEstrategia", request.IdEstrategia);
        parametros.Add("p_Nombre", request.Nombre.Trim());
        parametros.Add(
            "p_Descripcion",
            string.IsNullOrWhiteSpace(request.Descripcion)
                ? null
                : request.Descripcion.Trim()
        );

        try
        {
            var resultado = conexion.QuerySingle<int>(
                "SP_Actividad_Agregar",
                parametros,
                commandType: CommandType.StoredProcedure
            );

            return Ok(new
            {
                idActividad = resultado,
                mensaje = "Actividad agregada correctamente."
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
                mensaje = "Error al agregar la actividad.",
                detalle = ex.Message
            });
        }
    }


    [HttpGet("estrategia/{idEstrategia:int}")]
    public async Task<IActionResult> ListarPorEstrategia(int idEstrategia)
    {
        if (idEstrategia <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe indicar una estrategia válida."
            });
        }

        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection")
        );

        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstrategia", idEstrategia);

        try
        {
            var actividades = await conexion.QueryAsync<ActividadViewModel>(
                "SP_Actividad_Listar",
                parametros,
                commandType: CommandType.StoredProcedure
            );

            return Ok(actividades);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al consultar las actividades.",
                detalle = ex.Message
            });
        }
    }


}
