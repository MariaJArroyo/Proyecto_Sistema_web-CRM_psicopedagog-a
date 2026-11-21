using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/portal")]
[Authorize(Roles = "Encargado")]
public class PortalController : ControllerBase
{
    private readonly IServicioPortal _servicio;

    public PortalController(IServicioPortal servicio)
    {
        _servicio = servicio;
    }

    #region Inicio

    [HttpGet("inicio")]
    public async Task<ActionResult<PortalResponse>> ObtenerInicio([FromQuery] int? idEstudiante,CancellationToken cancelacion)
    {
        var resultado = await _servicio.ObtenerInicioAsync(User.ObtenerIdUsuario(),idEstudiante,cancelacion);

        return Ok(resultado);
    }

    #endregion

    #region Citas

    [HttpGet("citas")]
    public async Task<ActionResult<IReadOnlyList<PortalCitaResponse>>>
        ListarCitas([FromQuery] int idEstudiante, [FromQuery] string? filtro,CancellationToken cancelacion)
    {
        var resultado = await _servicio.ListarCitasAsync(User.ObtenerIdUsuario(), idEstudiante, filtro, cancelacion);

        return Ok(resultado);
    }

    #endregion

    #region Progreso

    [HttpGet("progreso")]
    public async Task<ActionResult<PortalProgresoResponse>> ListarProgreso([FromQuery] int idEstudiante,[FromQuery] string? filtroReportes, CancellationToken cancelacion)
    {
        var resultado = await _servicio.ListarProgresoAsync(
            User.ObtenerIdUsuario(), idEstudiante, filtroReportes,cancelacion);

        return Ok(resultado);
    }

    #endregion

    #region Tareas

    [HttpGet("tareas")]
    public async Task<ActionResult<IReadOnlyList<PortalTareaResponse>>>
        ListarTareas([FromQuery] int idEstudiante,[FromQuery] string? filtro,CancellationToken cancelacion)
    {
        var resultado = await _servicio.ListarTareasAsync(User.ObtenerIdUsuario(),idEstudiante,filtro,cancelacion);

        return Ok(resultado);
    }

    #endregion

    #region Pagos

    [HttpGet("pagos")]
    public async Task<ActionResult<IReadOnlyList<PortalPagoResponse>>>
        ListarPagos([FromQuery] int idEstudiante,[FromQuery] string? filtro,CancellationToken cancelacion)
    {
        var resultado = await _servicio.ListarPagosAsync( User.ObtenerIdUsuario(),idEstudiante,filtro,cancelacion);

        return Ok(resultado);
    }
    [HttpGet("pagos/{idPago:int}")]
    public async Task<ActionResult<PortalPagoDetalleResponse>> ObtenerPago(int idPago,
    [FromQuery] int idEstudiante,CancellationToken cancelacion)
    {
        var resultado = await _servicio.ObtenerPagoAsync( User.ObtenerIdUsuario(),idEstudiante,idPago,cancelacion);

        return Ok(resultado);
    }

    #endregion


    #region  tareas
    [HttpGet("tareas/{idTareaAsignacion:int}/archivo")]
    public async Task<IActionResult> DescargarArchivoTarea(int idTareaAsignacion, [FromQuery] int idEstudiante, CancellationToken cancelacion)
    {
        var archivo = await _servicio.AbrirArchivoTareaAsync(User.ObtenerIdUsuario(), idEstudiante, idTareaAsignacion, cancelacion);

        if (archivo is null)
        {
            return NotFound(new { mensaje = "El archivo no está disponible en el servidor." });
        }

        Response.Headers.CacheControl = "no-store";

        return File(archivo.Contenido, "application/pdf", archivo.NombreDescarga);
    }
    #endregion

    #region Recursos

    // Guias, PDF, actividades y enlaces publicados. Sin RutaArchivo.
    [HttpGet("recursos")]
    public async Task<ActionResult<IReadOnlyList<PortalRecursoResponse>>> ListarRecursos(CancellationToken cancelacion)
    {
        var resultado = await _servicio.ListarRecursosAsync(User.ObtenerIdUsuario(), cancelacion);

        return Ok(resultado);
    }

    // Lo publicado y activo, para cualquier encargado con un estudiante activo
    // (SP_Portal_ObtenerRecurso). Lo demas sale como 400 con su mensaje.
    [HttpGet("recursos/{idMaterial:int}/archivo")]
    public async Task<IActionResult> DescargarArchivoRecurso(int idMaterial, CancellationToken cancelacion)
    {
        var archivo = await _servicio.ObtenerArchivoRecursoAsync(User.ObtenerIdUsuario(), idMaterial, cancelacion);

        if (archivo is null)
        {
            return NotFound(new { mensaje = "El archivo no está disponible en el servidor." });
        }

        Response.Headers.CacheControl = "no-store";

        return File(archivo.Contenido, "application/pdf", archivo.NombreDescarga);
    }

    // 200 con { disponible, mensaje } tambien cuando el enlace esta caido
    [HttpGet("recursos/{idMaterial:int}/verificar-enlace")]
    public async Task<ActionResult<VerificacionEnlaceResponse>> VerificarEnlaceRecurso(int idMaterial, CancellationToken cancelacion)
    {
        var resultado = await _servicio.VerificarEnlaceRecursoAsync(User.ObtenerIdUsuario(), idMaterial, cancelacion);

        return Ok(resultado);
    }

    #endregion


    [HttpGet("reportes/{idReporte:int}")]
    public async Task<ActionResult<PortalReporteDetalleResponse>> ObtenerReporte(int idReporte,[FromQuery] int idEstudiante, CancellationToken cancelacion)
    {
        var resultado = await _servicio.ObtenerReporteAsync(User.ObtenerIdUsuario(), idEstudiante, idReporte, cancelacion);

        Response.Headers.CacheControl = "no-store";
        return Ok(resultado);
    }

}