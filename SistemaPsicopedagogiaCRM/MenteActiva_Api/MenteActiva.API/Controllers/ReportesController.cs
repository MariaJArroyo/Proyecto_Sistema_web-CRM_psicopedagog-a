using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Sin el rol Asistente, igual que Sesiones: el reporte reune la informacion
// clinica del menor (sesiones, avances, recomendaciones).
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

    #region ListarReportes

    [HttpGet]
    public async Task<IActionResult> ListarReportes(
        [FromQuery] int? idEstudiante,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken cancelacion)
    {
        var reportes = await _servicio.ListarAsync(
            idEstudiante,desde,hasta, cancelacion);

        return Ok(reportes);
    }

    #endregion

    #region
    [HttpPost]
    public async Task<IActionResult> GenerarReporte(
    [FromBody] MenteActiva.Api.Models.GenerarReporteRequest peticion,
    CancellationToken cancelacion)
    {
        var idUsuario =
            MenteActiva.Api.Infraestructura.ClaimsPrincipalExtensions
                .ObtenerIdUsuario(User);

        var reporte = await _servicio.GenerarAsync(idUsuario, peticion, cancelacion);

        return Ok(reporte);
    }
    #endregion

    #region ConsultarDetalleReporte


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

    #endregion
}