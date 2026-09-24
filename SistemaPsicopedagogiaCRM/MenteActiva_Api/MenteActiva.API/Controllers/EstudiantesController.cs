using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MenteActiva.Api.Models;
using System.Data;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstudiantesController : ControllerBase
{
    private readonly IConfiguration _config;

    public EstudiantesController(IConfiguration config)
    {
        _config = config;
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
    [HttpGet("{id}")]
    public IActionResult ConsultarFichaEstudiante(int id)
    {
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", id);

        try
        {
            var estudiante = conexion.QueryFirstOrDefault<FichaEstudianteResponse>(
                "SP_ConsultarFichaEstudiante_CRM",
                parametros,
                commandType: CommandType.StoredProcedure);

            if (estudiante == null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el estudiante."
                });
            }

            return Ok(estudiante);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al consultar la ficha del estudiante.",
                detalle = ex.Message
            });
        }
    }
    #endregion

}