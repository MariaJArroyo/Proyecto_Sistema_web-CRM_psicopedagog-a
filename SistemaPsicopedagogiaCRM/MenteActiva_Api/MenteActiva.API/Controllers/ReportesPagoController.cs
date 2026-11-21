using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Pagos que reportan las familias (SINPE o transferencia) y su revision.
// Dos publicos en el mismo controlador, separados por rol en cada accion:
//   - Encargado: reporta y consulta lo suyo  -> api/portal/...
//   - Panel: lista, ve el comprobante, aprueba o rechaza -> api/pagos/reportes/...
[ApiController]
public sealed class ReportesPagoController : ControllerBase
{
    private const string RolesPanel = "Administrador,Psicopedagoga,Asistente";

    private readonly IServicioReportesPago _servicio;

    public ReportesPagoController(IServicioReportesPago servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // PORTAL
    // =====================================================

    // multipart/form-data: idEstudiante, idMetodoPago, monto, fechaPago,
    // referencia, comentario y comprobante (archivo, opcional)
    [HttpPost("api/portal/pagos/{idPago:int}/reportes")]
    [Authorize(Roles = "Encargado")]
    [Consumes("multipart/form-data")]
    [TypeFilter(typeof(LimiteSubidaComprobante))]
    public async Task<IActionResult> Reportar(
        int idPago, [FromForm] ReportarPagoRequest peticion, CancellationToken cancelacion)
    {
        var id = await _servicio.ReportarAsync(User.ObtenerIdUsuario(), idPago, peticion, cancelacion);

        return Ok(new
        {
            idReportePago = id,
            mensaje = "¡Gracias! Recibimos su pago. La profesional lo revisará y le avisaremos aquí mismo."
        });
    }

    [HttpGet("api/portal/reportes-pago")]
    [Authorize(Roles = "Encargado")]
    public async Task<ActionResult<IReadOnlyList<PortalReportePagoResponse>>> ListarPortal(
        [FromQuery] int idEstudiante, CancellationToken cancelacion)
        => Ok(await _servicio.ListarPortalAsync(User.ObtenerIdUsuario(), idEstudiante, cancelacion));

    // =====================================================
    // PANEL
    // =====================================================

    // estado: Pendiente, Aprobado, Rechazado o vacio para todos
    [HttpGet("api/pagos/reportes")]
    [Authorize(Roles = RolesPanel)]
    public async Task<ActionResult<IReadOnlyList<ReportePagoResponse>>> Listar(
        [FromQuery] string? estado, CancellationToken cancelacion)
        => Ok(await _servicio.ListarAsync(estado, cancelacion));

    [HttpGet("api/pagos/reportes/pendientes/total")]
    [Authorize(Roles = RolesPanel)]
    public async Task<ActionResult<object>> ContarPendientes(CancellationToken cancelacion)
        => Ok(new { total = await _servicio.ContarPendientesAsync(cancelacion) });

    [HttpGet("api/pagos/reportes/{idReportePago:int}/comprobante")]
    [Authorize(Roles = RolesPanel)]
    public async Task<IActionResult> Comprobante(int idReportePago, CancellationToken cancelacion)
    {
        var archivo = await _servicio.AbrirComprobanteAsync(idReportePago, cancelacion);

        if (archivo is null)
        {
            return NotFound(new { mensaje = "El comprobante no está disponible en el servidor." });
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(archivo.Contenido, archivo.Tipo, archivo.NombreDescarga);
    }

    [HttpPut("api/pagos/reportes/{idReportePago:int}/aprobar")]
    [Authorize(Roles = RolesPanel)]
    public async Task<IActionResult> Aprobar(int idReportePago, CancellationToken cancelacion)
    {
        var idAbono = await _servicio.AprobarAsync(User.ObtenerIdUsuario(), idReportePago, cancelacion);

        return Ok(new { idAbono, mensaje = "Pago aprobado y registrado en el cobro." });
    }

    [HttpPut("api/pagos/reportes/{idReportePago:int}/rechazar")]
    [Authorize(Roles = RolesPanel)]
    public async Task<IActionResult> Rechazar(
        int idReportePago, [FromBody] RechazarReportePagoRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.RechazarAsync(User.ObtenerIdUsuario(), idReportePago, peticion, cancelacion);

        return Ok(new { mensaje = "Reporte rechazado. La familia verá el motivo en el portal." });
    }
}
