using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MenteActiva.Api.Controllers;

// Documentos del estudiante.
//   - Panel (Administrador y Psicopedagoga, igual que las areas y las sesiones:
//     pueden ser evaluaciones o informes con informacion clinica del menor):
//     api/estudiantes/{id}/documentos y api/estudiantes/documentos/{id}/...
//   - Portal (Encargado): api/portal/documentos, solo los visibles
[ApiController]
public sealed class DocumentosEstudianteController : ControllerBase
{
    private const string RolesPanel = "Administrador,Psicopedagoga";

    private readonly IServicioDocumentos _servicio;

    public DocumentosEstudianteController(IServicioDocumentos servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // PANEL
    // =====================================================

    [HttpGet("api/estudiantes/{idEstudiante:int}/documentos")]
    [Authorize(Roles = RolesPanel)]
    public async Task<ActionResult<IReadOnlyList<DocumentoEstudianteResponse>>> Listar(
        int idEstudiante, CancellationToken cancelacion)
        => Ok(await _servicio.ListarAsync(idEstudiante, cancelacion));

    // multipart/form-data: archivo, titulo, descripcion, visibleEnPortal
    [HttpPost("api/estudiantes/{idEstudiante:int}/documentos")]
    [Authorize(Roles = RolesPanel)]
    [Consumes("multipart/form-data")]
    [TypeFilter(typeof(LimiteSubidaDocumento))]
    public async Task<IActionResult> Subir(
        int idEstudiante, [FromForm] SubirDocumentoRequest peticion, CancellationToken cancelacion)
    {
        var id = await _servicio.SubirAsync(User.ObtenerIdUsuario(), idEstudiante, peticion, cancelacion);

        return Ok(new
        {
            idDocumento = id,
            mensaje = peticion.VisibleEnPortal
                ? "Documento subido. La familia ya lo ve en el portal."
                : "Documento subido. Solo lo ve el personal."
        });
    }

    [HttpGet("api/estudiantes/documentos/{idDocumento:int}/archivo")]
    [Authorize(Roles = RolesPanel)]
    public async Task<IActionResult> Archivo(int idDocumento, CancellationToken cancelacion)
        => Servir(await _servicio.AbrirAsync(idDocumento, cancelacion));

    [HttpPut("api/estudiantes/documentos/{idDocumento:int}/visibilidad")]
    [Authorize(Roles = RolesPanel)]
    public async Task<IActionResult> CambiarVisibilidad(
        int idDocumento, [FromBody] CambiarVisibilidadDocumentoRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.CambiarVisibilidadAsync(User.ObtenerIdUsuario(), idDocumento, peticion.Visible, cancelacion);

        return Ok(new
        {
            mensaje = peticion.Visible
                ? "La familia ya puede ver el documento en el portal."
                : "El documento ya no se muestra en el portal."
        });
    }

    [HttpDelete("api/estudiantes/documentos/{idDocumento:int}")]
    [Authorize(Roles = RolesPanel)]
    public async Task<IActionResult> Eliminar(int idDocumento, CancellationToken cancelacion)
    {
        await _servicio.EliminarAsync(User.ObtenerIdUsuario(), idDocumento, cancelacion);

        return Ok(new { mensaje = "Documento eliminado." });
    }

    // =====================================================
    // PORTAL
    // =====================================================

    [HttpGet("api/portal/documentos")]
    [Authorize(Roles = "Encargado")]
    public async Task<ActionResult<IReadOnlyList<PortalDocumentoResponse>>> ListarPortal(
        [FromQuery] int idEstudiante, CancellationToken cancelacion)
        => Ok(await _servicio.ListarPortalAsync(User.ObtenerIdUsuario(), idEstudiante, cancelacion));

    [HttpGet("api/portal/documentos/{idDocumento:int}/archivo")]
    [Authorize(Roles = "Encargado")]
    public async Task<IActionResult> ArchivoPortal(
        int idDocumento, [FromQuery] int idEstudiante, CancellationToken cancelacion)
        => Servir(await _servicio.AbrirPortalAsync(User.ObtenerIdUsuario(), idEstudiante, idDocumento, cancelacion));

    // ---------- auxiliares ----------

    private IActionResult Servir(DocumentoDescarga? archivo)
    {
        if (archivo is null)
        {
            return NotFound(new { mensaje = "El documento no está disponible." });
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(archivo.Contenido, archivo.Tipo, archivo.NombreDescarga);
    }
}

// Tope de la peticion solo para la subida de documentos (ver LimiteSubidaPdf)
public sealed class LimiteSubidaDocumento : IResourceFilter
{
    private const long MargenFormulario = 64 * 1024;

    public void OnResourceExecuting(ResourceExecutingContext contexto)
    {
        var http = contexto.HttpContext;
        var tope = AlmacenDocumentos.TamanoMaximoBytes + MargenFormulario;

        if (http.Request.ContentLength > tope)
        {
            contexto.Result = new BadRequestObjectResult(new
            {
                mensaje = $"El archivo no puede pasar de {AlmacenDocumentos.TamanoMaximoMb} MB."
            });
            return;
        }

        var limiteCuerpo = http.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (limiteCuerpo is { IsReadOnly: false })
        {
            limiteCuerpo.MaxRequestBodySize = tope;
        }

        if (http.Features.Get<IFormFeature>()?.Form is null)
        {
            http.Features.Set<IFormFeature>(new FormFeature(http.Request, new FormOptions
            {
                MultipartBodyLengthLimit = tope
            }));
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext contexto)
    {
    }
}
