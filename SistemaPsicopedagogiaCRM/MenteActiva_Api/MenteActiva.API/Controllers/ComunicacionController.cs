using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Solo traduce HTTP <-> servicio. Las reglas estan en ServicioComunicacion y en
// los procedimientos; los errores los convierte ManejadorExcepciones.
//
// Reemplaza al controlador del mismo nombre que abria su propia conexion y
// llamaba a un procedimiento que no existia.
//
// Escribir correo queda en Administrador y Psicopedagoga: sale a nombre del
// consultorio y lo recibe una familia. El historial lo ven los tres, porque el
// Asistente atiende la comunicacion con los encargados y necesita saber que ya
// se les escribio.
[ApiController]
[Route("api/comunicacion")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public sealed class ComunicacionController : ControllerBase
{
    private const string RolesEnvio = "Administrador,Psicopedagoga";

    private readonly IServicioComunicacion _servicio;

    public ComunicacionController(IServicioComunicacion servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    // Destinatarios con sus estudiantes, plantillas activas y marcadores, en una
    // sola llamada: es lo que la pestania necesita al abrir.
    [HttpGet("catalogos")]
    public async Task<ActionResult<CatalogosCorreoResponse>> ObtenerCatalogos(
        CancellationToken cancelacion)
        => Ok(await _servicio.ObtenerCatalogosAsync(cancelacion));

    // Las citas del estudiante que se pueden mencionar en un correo. Va aparte
    // de los catalogos a proposito: traerlas todas al abrir la pantalla seria
    // cargar las citas de todos los estudiantes para usar, como mucho, las de
    // uno. Pide los mismos roles que el envio, porque solo sirve para redactar.
    [HttpGet("citas")]
    [Authorize(Roles = RolesEnvio)]
    public async Task<ActionResult<IReadOnlyList<CitaCorreoResponse>>> ListarCitasEstudiante(
        [FromQuery] int idEstudiante, CancellationToken cancelacion)
        => Ok(await _servicio.ListarCitasEstudianteAsync(idEstudiante, cancelacion));

    // soloManuales=true es el historial de la pestania de correo; en false trae
    // tambien los avisos automaticos, que es lo que usa la pestania de
    // recordatorios.
    [HttpGet("notificaciones")]
    public async Task<ActionResult<IReadOnlyList<NotificacionResponse>>> ListarNotificaciones(
        [FromQuery] bool soloManuales = false, [FromQuery] int? limite = null,
        CancellationToken cancelacion = default)
        => Ok(await _servicio.ListarNotificacionesAsync(soloManuales, limite, cancelacion));

    // =====================================================
    // ESCRITURAS
    // =====================================================

    // No escribe nada: devuelve el texto ya sustituido para que la persona vea
    // lo que va a salir antes de mandarlo, y dice que marcadores lo bloquean.
    [HttpPost("correo/vista-previa")]
    [Authorize(Roles = RolesEnvio)]
    public async Task<ActionResult<VistaPreviaCorreoResponse>> VistaPrevia(
        [FromBody] EnviarCorreoRequest peticion, CancellationToken cancelacion)
        => Ok(await _servicio.VistaPreviaAsync(peticion, cancelacion));

    [HttpPost("correo")]
    [Authorize(Roles = RolesEnvio)]
    public async Task<ActionResult<CorreoEnviadoResponse>> EnviarCorreo(
        [FromBody] EnviarCorreoRequest peticion, CancellationToken cancelacion)
        => Ok(await _servicio.EnviarCorreoAsync(User.ObtenerIdUsuario(), peticion, cancelacion));
}
