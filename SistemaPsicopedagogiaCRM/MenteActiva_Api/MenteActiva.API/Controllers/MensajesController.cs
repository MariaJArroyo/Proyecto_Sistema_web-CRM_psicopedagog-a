using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Mensajeria interna (HU-M10-4). Controlador aparte de ComunicacionController
// porque es el unico de M10 donde entra el rol Encargado, y los [Authorize] se
// combinan con AND: puesto sobre una clase que exige los tres roles del panel,
// el encargado quedaria afuera.
//
// Bandeja del consultorio (HU-M10-4b): el encargado escribe sin elegir a quien y
// su mensaje lo ve el personal cuyo rol tenga acceso, que se configura en
// MensajeriaAccesoController. Los tres roles del panel quedan en el [Authorize]
// porque cualquiera puede recibir el acceso; quien no lo tiene recibe 400 con el
// mensaje del procedimiento, y la pantalla pregunta antes con GET acceso.
//
// Los cuatro roles usan los mismos endpoints: todo va con el id del usuario
// autenticado y los procedimientos validan quien puede ver cada hilo.
[ApiController]
[Route("api/mensajes")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente,Encargado")]
public sealed class MensajesController : ControllerBase
{
    private readonly IServicioMensajes _servicio;

    public MensajesController(IServicioMensajes servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    // Mis conversaciones, con el ultimo mensaje y cuantas van sin leer
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HiloResponse>>> ListarHilos(
        CancellationToken cancelacion)
        => Ok(await _servicio.ListarHilosAsync(User.ObtenerIdUsuario(), cancelacion));

    // Abrir una conversacion la marca como leida: es GET pero escribe, y es a
    // proposito, porque abrir un hilo ES leerlo.
    [HttpGet("{idHilo:int}")]
    public async Task<ActionResult<IReadOnlyList<MensajeResponse>>> AbrirHilo(
        int idHilo, CancellationToken cancelacion)
        => Ok(await _servicio.AbrirHiloAsync(User.ObtenerIdUsuario(), idHilo, cancelacion));

    // Los encargados con portal a los que el personal con acceso puede escribir.
    // Un encargado ya no elige destinatario: para el responde 400.
    [HttpGet("destinatarios")]
    public async Task<ActionResult<IReadOnlyList<DestinatarioMensajeResponse>>> ListarDestinatarios(
        CancellationToken cancelacion)
        => Ok(await _servicio.ListarDestinatariosAsync(User.ObtenerIdUsuario(), cancelacion));

    [HttpGet("sin-leer")]
    public async Task<ActionResult<object>> ContarSinLeer(CancellationToken cancelacion)
        => Ok(new { sinLeer = await _servicio.ContarSinLeerAsync(User.ObtenerIdUsuario(), cancelacion) });

    // Si quien pregunta ve la bandeja del consultorio. Para que el panel esconda
    // la pestana sin provocar el 400. Un encargado siempre da false: el ve sus
    // propias conversaciones, no la bandeja.
    [HttpGet("acceso")]
    public async Task<ActionResult<object>> ConsultarAcceso(CancellationToken cancelacion)
        => Ok(new { tieneAcceso = await _servicio.TieneAccesoAsync(User.ObtenerIdUsuario(), cancelacion) });

    // =====================================================
    // ESCRITURAS
    // =====================================================

    // Sin idHilo abre una conversacion nueva; con idHilo responde en esa.
    [HttpPost]
    public async Task<ActionResult<MensajeEnviadoResponse>> Enviar(
        [FromBody] EnviarMensajeRequest peticion, CancellationToken cancelacion)
        => Ok(await _servicio.EnviarAsync(
            User.ObtenerIdUsuario(), User.IsInRole("Encargado"), peticion, cancelacion));
}
