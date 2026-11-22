using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Entregas de tareas. Dos publicos en el mismo controlador, separados por rol:
//   - Encargado: entrega la tarea y ve su propio archivo -> api/portal/tareas/...
//   - Panel: ve quien entrego, abre el archivo y revisa   -> api/tareas/...
[ApiController]
public sealed class EntregasTareaController : ControllerBase
{
    private const string RolesPanel = "Administrador,Psicopedagoga,Asistente";

    private readonly IServicioEntregasTarea _servicio;

    public EntregasTareaController(IServicioEntregasTarea servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // PORTAL
    // =====================================================

    // multipart/form-data: idEstudiante, comentario y archivo (opcional)
    [HttpPost("api/portal/tareas/{idTareaAsignacion:int}/entrega")]
    [Authorize(Roles = "Encargado")]
    [Consumes("multipart/form-data")]
    [TypeFilter(typeof(LimiteSubidaEntregaTarea))]
    public async Task<IActionResult> Entregar(
        int idTareaAsignacion, [FromForm] EntregarTareaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.EntregarAsync(User.ObtenerIdUsuario(), idTareaAsignacion, peticion, cancelacion);

        return Ok(new { mensaje = "¡Tarea entregada! La profesional la revisará pronto." });
    }

    [HttpGet("api/portal/tareas/{idTareaAsignacion:int}/entrega/archivo")]
    [Authorize(Roles = "Encargado")]
    public async Task<IActionResult> ArchivoPortal(
        int idTareaAsignacion, [FromQuery] int idEstudiante, CancellationToken cancelacion)
        => Archivo(await _servicio.AbrirArchivoPortalAsync(
            User.ObtenerIdUsuario(), idEstudiante, idTareaAsignacion, cancelacion));

    // =====================================================
    // PANEL
    // =====================================================

    // Cada estudiante asignado con el estado de su entrega
    [HttpGet("api/tareas/{idTarea:int}/entregas")]
    [Authorize(Roles = RolesPanel)]
    public async Task<ActionResult<IReadOnlyList<EntregaTareaResponse>>> Listar(
        int idTarea, CancellationToken cancelacion)
        => Ok(await _servicio.ListarPorTareaAsync(idTarea, cancelacion));

    [HttpGet("api/tareas/entregas/{idTareaAsignacion:int}/archivo")]
    [Authorize(Roles = RolesPanel)]
    public async Task<IActionResult> ArchivoPanel(int idTareaAsignacion, CancellationToken cancelacion)
        => Archivo(await _servicio.AbrirArchivoAsync(idTareaAsignacion, cancelacion));

    // { aprobada: true } la da por revisada; { aprobada: false, comentario }
    // la devuelve a la familia como "Por corregir"
    [HttpPut("api/tareas/entregas/{idTareaAsignacion:int}/revision")]
    [Authorize(Roles = RolesPanel)]
    public async Task<IActionResult> Revisar(
        int idTareaAsignacion, [FromBody] RevisarEntregaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.RevisarAsync(User.ObtenerIdUsuario(), idTareaAsignacion, peticion, cancelacion);

        return Ok(new
        {
            mensaje = peticion.Aprobada
                ? "Entrega marcada como revisada."
                : "Tarea devuelta. La familia verá su comentario en el portal."
        });
    }

    private IActionResult Archivo(ComprobanteDescarga? archivo)
    {
        if (archivo is null)
        {
            return NotFound(new { mensaje = "El archivo de la entrega no está disponible." });
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(archivo.Contenido, archivo.Tipo, archivo.NombreDescarga);
    }
}
