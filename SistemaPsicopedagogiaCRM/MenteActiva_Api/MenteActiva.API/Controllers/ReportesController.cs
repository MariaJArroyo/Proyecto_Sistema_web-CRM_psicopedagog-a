using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Psicopedagoga")]
public class ReportesController : ControllerBase
{
    private readonly IServicioReportes _servicio;

    public ReportesController(IServicioReportes servicio)
    {
        _servicio = servicio;
    }

    #region listar
    [HttpGet]
    public async Task<IActionResult> ListarReportes(
        [FromQuery] int? idEstudiante,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] string? buscar,
        [FromQuery] string? estado,
        [FromQuery] string? orden,
        CancellationToken cancelacion)
    {
        var reportes = await _servicio.ListarAsync(
            idEstudiante,
            desde,
            hasta,
            buscar,
            estado ?? "activos",
            orden ?? "az",
            cancelacion);

        return Ok(reportes);
    }
    #endregion

    #region consultar
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ConsultarDetalleReporte(
        int id,
        CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El reporte indicado no es válido."
            });
        }

        var reporte = await _servicio.ObtenerAsync(id, cancelacion);

        return Ok(reporte);
    }
    #endregion region

    #region generar reporte
    [HttpPost]
    public async Task<IActionResult> GenerarReporte(
        [FromBody] GenerarReporteRequest peticion,
        CancellationToken cancelacion)
    {
        var reporte = await _servicio.GenerarAsync(
            User.ObtenerIdUsuario(),
            peticion,
            cancelacion);

        if (reporte is null)
        {
            return BadRequest(new
            {
                mensaje =
                    "No existe información suficiente para las secciones " +
                    "seleccionadas en el período indicado. " +
                    "Revise que existan datos para todas las secciones marcadas."
            });
        }

        return Ok(reporte);
    }
#endregion

    #region cambiar estado
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(
        int id,[FromBody] CambiarEstadoReporteRequest peticion,CancellationToken cancelacion)
    {
        if (id <= 0 || !peticion.Activo.HasValue)
        {
            return BadRequest(new
            {
                mensaje = "Indique un reporte y estado válidos."
            });
        }

        await _servicio.CambiarEstadoAsync(id,peticion.Activo.Value,cancelacion);

        return Ok(new
        {
            mensaje = peticion.Activo.Value
                ? "Reporte reactivado correctamente."
                : "Reporte desactivado correctamente."
        });
    #endregion  

    }
}