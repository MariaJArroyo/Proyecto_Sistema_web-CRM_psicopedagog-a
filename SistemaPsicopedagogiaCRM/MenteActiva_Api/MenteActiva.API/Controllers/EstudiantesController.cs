using Dapper;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Security.Claims;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstudiantesController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly RepositorioEstudiante _repositorioEstudiante;

    public EstudiantesController(
        IConfiguration config,
        RepositorioEstudiante repositorioEstudiante)
    {
        _config = config;
        _repositorioEstudiante = repositorioEstudiante;
    }

    #region VerEstudiante
    [HttpGet]
    public IActionResult ConsultarEstudiantes()
    {
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        try
        {
            var estudiantes = conexion.Query<EstudianteResponse>(
                "SP_ConsultarEstudiantes_CRM",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return Ok(estudiantes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al consultar los estudiantes.",
                detalle = ex.Message
            });
        }
    }
    #endregion


    #region ver ficha   
    [HttpGet("{id:int}")]
    public IActionResult ConsultarFichaEstudiante(int id)
    {
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", id);

        using var resultados = conexion.QueryMultiple(
            "SP_ConsultarFichaEstudiante_CRM",
            parametros,
            commandType: CommandType.StoredProcedure);

        var estudiante =
            resultados.ReadFirstOrDefault<FichaEstudianteResponse>();

        if (estudiante == null)
        {
            return NotFound(new
            {
                mensaje = "No se encontró el estudiante."
            });
        }

        estudiante.Areas =
            resultados.Read<AreaEstudianteResponse>().ToList();

        return Ok(estudiante);
    }

    #endregion

    #region AgregarAreadeDificultad

    [HttpPost("{id:int}/areas")]
    public async Task<IActionResult> AgregarArea(
        int id,
        [FromBody] AgregarAreaEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El estudiante indicado no es válido."
            });
        }

        await _repositorioEstudiante.AgregarAreaAsync(
            id,
            request.NombreArea,
            request.Observacion,
            cancelacion);

        return Ok(new
        {
            mensaje = "Área de dificultad registrada correctamente."
        });
    }

    #endregion

    #region EditarAreadeDificultad

    [HttpPut("{id:int}/areas/{idArea:int}")]
    public async Task<IActionResult> EditarArea(
        int id,
        int idArea,
        [FromBody] AgregarAreaEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0 || idArea <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Los datos indicados no son válidos."
            });
        }

        await _repositorioEstudiante.EditarAreaAsync(
            id,
            idArea,
            request.NombreArea,
            request.Observacion,
            cancelacion);

        return Ok(new
        {
            mensaje = "Área de dificultad actualizada correctamente."
        });
    }

    #endregion

    #region EditarEstudiante

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditarEstudiante(
        int id,
        [FromBody] EditarEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new { mensaje = "El estudiante indicado no es válido." });
        }

        if (request.FechaNacimiento?.Date > DateTime.Today)
        {
            return BadRequest(new { mensaje = "La fecha de nacimiento no puede ser futura." });
        }

        await _repositorioEstudiante.EditarFichaAsync(
            ObtenerIdUsuarioActual(),
            id,
            request,
            cancelacion);

        return Ok(new { mensaje = "Ficha del estudiante actualizada correctamente." });
    }

    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    #endregion

}