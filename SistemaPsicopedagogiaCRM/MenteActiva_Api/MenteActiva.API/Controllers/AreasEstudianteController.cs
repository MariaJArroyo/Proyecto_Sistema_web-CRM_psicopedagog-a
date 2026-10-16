using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrador,Psicopedagoga")]
[Route("api/estudiantes/{idEstudiante:int}/areas")]
public sealed class AreasEstudianteController(IConfiguration config) : ControllerBase
{
  

    #region Listar áreas actuales e inactivas

    [HttpGet("estado-lista")]
    public async Task<IActionResult> Listar(int idEstudiante, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Seleccione un estudiante válido."
            });
        }

        try
        {
            await using var conexion = new MySqlConnection(
                config.GetConnectionString("DefaultConnection"));

            var areas = await conexion.QueryAsync<AreaEstadoResponse>(
                new CommandDefinition(
                    "SP_Estudiante_ListarAreasEstado",
                    new
                    {
                        p_IdEstudiante = idEstudiante
                    },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancelacion));

            return Ok(areas);
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    #endregion

    #region Desactivar o reactivar área

    [HttpPatch("{idArea:int}/estado")]
    public async Task<IActionResult> Cambiar(int idEstudiante, int idArea, EstadoAreaRequest modelo, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0 || idArea <= 0 || !modelo.Activo.HasValue)
        {
            return BadRequest(new
            {
                mensaje = "Indique un estudiante, área y estado válidos."
            });
        }

        try
        {
            await using var conexion = new MySqlConnection(
                config.GetConnectionString("DefaultConnection"));

            await conexion.ExecuteAsync(
                new CommandDefinition(
                    "SP_Estudiante_CambiarEstadoArea",
                    new
                    {
                        p_IdEstudiante = idEstudiante,
                        p_IdAreaDificultad = idArea,
                        p_Activo = modelo.Activo.Value
                    },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancelacion));

            return Ok(new
            {
                mensaje = modelo.Activo.Value
                    ? "Área reactivada correctamente."
                    : "Área marcada como trabajada."
            });
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    #endregion
}