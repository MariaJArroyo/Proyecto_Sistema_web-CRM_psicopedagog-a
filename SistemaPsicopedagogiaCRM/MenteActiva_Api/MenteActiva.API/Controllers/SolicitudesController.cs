using Dapper;
using MenteActiva.Api.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Security.Claims;

namespace MenteActiva.Api.Controllers;

// Pestaña Solicitudes del panel. Hereda la politica global: exige sesion.
[ApiController]
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private readonly IConfiguration _config;

    public SolicitudesController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public IActionResult ConsultarSolicitudes()
    {
        using var conexion = CrearConexion();

        return Ok(conexion.Query<SolicitudResponse>(
            "SP_ConsultarSolicitudes_CRM",
            commandType: CommandType.StoredProcedure).ToList());
    }

    [HttpGet("{id:int}")]
    public IActionResult ObtenerSolicitud(int id)
    {
        using var conexion = CrearConexion();

        var parametros = new DynamicParameters();
        parametros.Add("p_IdSolicitud", id);

        // Dos resultados: la solicitud y los clientes que coinciden
        using var resultados = conexion.QueryMultiple(
            "SP_ObtenerSolicitud_CRM",
            parametros,
            commandType: CommandType.StoredProcedure);

        var solicitud = resultados.ReadFirstOrDefault<SolicitudDetalleResponse>();

        if (solicitud is null)
        {
            return NotFound(new { mensaje = "La solicitud no existe." });
        }

        solicitud.Coincidencias = resultados.Read<ClienteCoincidenteResponse>().ToList();

        return Ok(solicitud);
    }

    [HttpPut("{id:int}/estado")]
    public IActionResult CambiarEstado(int id, [FromBody] CambiarEstadoSolicitudRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { mensaje = "Revise los datos." });
        }

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", ObtenerIdUsuarioActual());
        parametros.Add("p_IdSolicitud", id);
        parametros.Add("p_IdEstadoSolicitud", request.IdEstadoSolicitud);
        parametros.Add("p_NotaInterna", request.NotaInterna);

        return EjecutarSp(
            "SP_CambiarEstadoSolicitud_CRM",
            parametros,
            "Solicitud actualizada correctamente.",
            "Error al actualizar la solicitud.");
    }

    [HttpPut("{id:int}/vincular")]
    public IActionResult VincularCliente(int id, [FromBody] VincularSolicitudRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { mensaje = "Debe indicar el cliente." });
        }

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", ObtenerIdUsuarioActual());
        parametros.Add("p_IdSolicitud", id);
        parametros.Add("p_IdEncargado", request.IdEncargado);

        return EjecutarSp(
            "SP_VincularSolicitudCliente_CRM",
            parametros,
            "Solicitud vinculada al cliente.",
            "Error al vincular la solicitud.");
    }

    [HttpGet("pendientes")]
    public IActionResult ContarPendientes()
    {
        using var conexion = CrearConexion();

        var pendientes = conexion.ExecuteScalar<int>(
            "SP_ContarSolicitudesPendientes_CRM",
            commandType: CommandType.StoredProcedure);

        return Ok(new { pendientes });
    }

    [HttpGet("estados")]
    public IActionResult ListarEstados()
    {
        using var conexion = CrearConexion();

        return Ok(conexion.Query<EstadoSolicitudResponse>(
            "SP_ListarEstadosSolicitud_CRM",
            commandType: CommandType.StoredProcedure).ToList());
    }

    #region Auxiliares

    private MySqlConnection CrearConexion()
        => new(_config.GetConnectionString("DefaultConnection"));

    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IActionResult EjecutarSp(string nombreSp, DynamicParameters parametros, string mensajeOk, string mensajeError)
    {
        using var conexion = CrearConexion();

        try
        {
            conexion.Execute(nombreSp, parametros, commandType: CommandType.StoredProcedure);
            return Ok(new { mensaje = mensajeOk });
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = mensajeError, detalle = ex.Message });
        }
    }

    #endregion
}