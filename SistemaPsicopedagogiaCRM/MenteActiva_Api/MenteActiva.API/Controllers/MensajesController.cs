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
// El Asistente SI entra, como en la agenda y en los pagos (decision D-G). Es mas
// abierto que M5, donde quedo fuera porque la sesion lleva la nota clinica del
// menor, y es a proposito.
//
// Los cuatro roles usan los mismos endpoints: todo va con el id del usuario
// autenticado y los procedimientos validan que participe del hilo. Nadie ve una
// conversacion que no sea suya, venga del panel o del portal.
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

    // A quien le puedo escribir: si soy del consultorio, los encargados con
    // portal; si soy encargado, la gente del consultorio.
    [HttpGet("destinatarios")]
    public async Task<ActionResult<IReadOnlyList<DestinatarioMensajeResponse>>> ListarDestinatarios(
        CancellationToken cancelacion)
        => Ok(await _servicio.ListarDestinatariosAsync(User.ObtenerIdUsuario(), cancelacion));

    [HttpGet("sin-leer")]
    public async Task<ActionResult<object>> ContarSinLeer(CancellationToken cancelacion)
        => Ok(new { sinLeer = await _servicio.ContarSinLeerAsync(User.ObtenerIdUsuario(), cancelacion) });

    // =====================================================
    // ESCRITURAS
    // =====================================================

    // Sin idHilo abre una conversacion nueva; con idHilo responde en esa.
    [HttpPost]
    public async Task<ActionResult<MensajeEnviadoResponse>> Enviar(
        [FromBody] EnviarMensajeRequest peticion, CancellationToken cancelacion)
        => Ok(await _servicio.EnviarAsync(User.ObtenerIdUsuario(), peticion, cancelacion));
}
