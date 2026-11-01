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
    private readonly IWebHostEnvironment _entorno;

    public PortalController(
        IServicioPortal servicio,
        IWebHostEnvironment entorno)
    {
        _servicio = servicio;
        _entorno = entorno;
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
        public async Task<IActionResult> DescargarArchivoTarea(int idTareaAsignacion,[FromQuery] int idEstudiante,CancellationToken cancelacion)
        {
            var archivo = await _servicio.ObtenerArchivoTareaAsync(User.ObtenerIdUsuario(),idEstudiante, idTareaAsignacion,cancelacion);

            var nombre = archivo.RutaArchivo.Trim();

            // RutaArchivo debe contener únicamente el nombre del archivo.
            if (string.IsNullOrWhiteSpace(nombre)
                || nombre is "." or ".."
                || Path.IsPathRooted(nombre)
                || nombre.IndexOfAny(new[] { '/', '\\', ':' }) >= 0
                || nombre.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return NotFound(new
                {
                    mensaje = "La ruta del archivo no es válida."
                });
            }

            var carpeta = Path.Combine(_entorno.ContentRootPath, "Archivos", "Materiales");

            var rutaArchivo = Path.Combine(carpeta, nombre);
            var informacion = new FileInfo(rutaArchivo);

            if (!informacion.Exists || informacion.LinkTarget is not null)
            {
                return NotFound(new
                {
                    mensaje = "El archivo no está disponible en el servidor."
                });
            }

            try
            {
                var flujo = new FileStream(rutaArchivo, FileMode.Open,FileAccess.Read,FileShare.Read);

                Response.Headers.CacheControl = "no-store";

                return File(flujo,"application/octet-stream",nombre);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new
                {
                    mensaje = "El archivo ya no está disponible."
                });
            }
            catch (DirectoryNotFoundException)
            {
                return NotFound(new
                {
                    mensaje = "El archivo ya no está disponible."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo acceder al archivo."
                });
            }
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