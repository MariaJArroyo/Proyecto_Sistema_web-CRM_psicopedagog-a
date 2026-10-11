
using Dapper;
using MenteActiva.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Security.Claims;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/planes")]
public class PlanesController : ControllerBase
{
    private readonly IConfiguration _config;

    public PlanesController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ObtenerTodos()
    {
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();

        parametros.Add("p_IdEstudiante", null, DbType.Int32);
        parametros.Add("p_IdEstadoPlan", null, DbType.Int32);

        var resultado = await conexion.QueryAsync<PlanResponse>(
            "SP_Plan_Listar",
            parametros,
            commandType: CommandType.StoredProcedure);

        return Ok(resultado);
    }

    [HttpPost]
    public IActionResult Crear([FromBody] PlanCrearRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensaje = "Revise los datos del plan."
            });
        }

        if (request.IdEstudiante <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe seleccionar un estudiante."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            return BadRequest(new
            {
                mensaje = "El título del plan es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ObjetivoGeneral))
        {
            return BadRequest(new
            {
                mensaje = "El objetivo general es obligatorio."
            });
        }

        if (request.Observaciones?.Length > 1000)
        {
            return BadRequest(new
            {
                mensaje = "Las observaciones no pueden superar los 1000 caracteres."
            });
        }

        if (request.FechaFin.HasValue &&
            request.FechaFin.Value.Date < request.FechaInicio.Date)
        {
            return BadRequest(new
            {
                mensaje = "La fecha de finalización no puede ser anterior a la fecha de inicio."
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
        parametros.Add("p_IdEstudiante", request.IdEstudiante);
        parametros.Add("p_Titulo", request.Titulo.Trim());
        parametros.Add("p_ObjetivoGeneral", request.ObjetivoGeneral.Trim());
        parametros.Add(
            "p_Observaciones",
            string.IsNullOrWhiteSpace(request.Observaciones)
                ? null
                : request.Observaciones.Trim());
        parametros.Add("p_FechaInicio", request.FechaInicio.Date);
        parametros.Add("p_FechaFin", request.FechaFin?.Date);
        parametros.Add("p_IdEstadoPlan", request.IdEstadoPlan);
        parametros.Add("p_EstrategiasJson", null);

        try
        {
            conexion.Execute(
                "SP_Plan_Crear",
                parametros,
                commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                mensaje = "Plan de intervención creado correctamente."
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
                mensaje = "Error al crear el plan.",
                detalle = ex.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult Editar(
        int id,
        [FromBody] PlanEditarRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensaje = "Revise los datos del plan."
            });
        }

        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El plan indicado no es válido."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            return BadRequest(new
            {
                mensaje = "El título del plan es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(request.ObjetivoGeneral))
        {
            return BadRequest(new
            {
                mensaje = "El objetivo general es obligatorio."
            });
        }

        if (request.Observaciones?.Length > 1000)
        {
            return BadRequest(new
            {
                mensaje = "Las observaciones no pueden superar los 1000 caracteres."
            });
        }

        if (request.FechaFin.HasValue &&
            request.FechaFin.Value.Date < request.FechaInicio.Date)
        {
            return BadRequest(new
            {
                mensaje = "La fecha de finalización no puede ser anterior a la fecha de inicio."
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
        parametros.Add("p_IdPlan", id);
        parametros.Add("p_Titulo", request.Titulo.Trim());
        parametros.Add("p_ObjetivoGeneral", request.ObjetivoGeneral.Trim());
        parametros.Add(
            "p_Observaciones",
            string.IsNullOrWhiteSpace(request.Observaciones)
                ? null
                : request.Observaciones.Trim());
        parametros.Add("p_FechaInicio", request.FechaInicio.Date);
        parametros.Add("p_FechaFin", request.FechaFin?.Date);

        try
        {
            conexion.Execute(
                "SP_Plan_Editar",
                parametros,
                commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                mensaje = "Plan de intervención actualizado correctamente."
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
                mensaje = "Error al editar el plan.",
                detalle = ex.Message
            });
        }
    }




    [HttpPut("{id:int}/estado")]
    public IActionResult CambiarEstado(
        int id,
        [FromBody] PlanCambiarEstadoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensaje = "Revise los datos del estado."
            });
        }

        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El plan indicado no es válido."
            });
        }

        if (request.IdEstadoPlan != 2 &&
    request.IdEstadoPlan != 5)
        {
            return BadRequest(new
            {
                mensaje = "Solo se permite activar o inactivar planes."
            });
        }

        var idUsuario = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

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
        parametros.Add("p_IdPlan", id);
        parametros.Add("p_IdEstadoPlan", request.IdEstadoPlan);

        try
        {
            conexion.Execute(
                "SP_Plan_CambiarEstado",
                parametros,
                commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                mensaje = "Plan de intervención inactivado correctamente."
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
                mensaje = "Error al inactivar el plan.",
                detalle = ex.Message
            });
        }
    }




    [HttpDelete("{id:int}")]
    public IActionResult Eliminar(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El identificador del plan no es válido."
            });
        }

        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_IdPlan", id);

        try
        {
            conexion.Execute(
                "SP_Plan_Eliminar",
                parametros,
                commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                mensaje = "Plan de intervención eliminado correctamente."
            });
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                mensaje = "Ocurrió un error al eliminar el plan."
            });
        }
    }




    [HttpGet("{id:int}/historial")]
    [AllowAnonymous]
    public async Task<IActionResult> Historial(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El plan indicado no es válido."
            });
        }

        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();

        parametros.Add("p_IdPlan", id);

        var resultado = await conexion.QueryAsync<PlanHistorialResponse>(
            "SP_Plan_Historial",
            parametros,
            commandType: CommandType.StoredProcedure);

        return Ok(resultado);
    }
}
