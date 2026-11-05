using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Pestania Solicitudes del panel. Solo traduce HTTP <-> servicio: las reglas
// estan en los procedimientos y los errores los convierte ManejadorExcepciones.
//
// Antes este controlador abria su propia conexion a MySQL, armaba los
// DynamicParameters y llevaba su propio try/catch, lo que choca con RG-92 y
// RG-117. Molde del arreglo: ComunicacionController, al que se le hizo lo mismo.
//
// Los tres roles del panel entran. Una solicitud de contacto trae el nombre, el
// telefono y el correo de una familia, asi que el rol Encargado no tiene nada
// que ver aqui: antes el controlador solo heredaba la politica global, que pide
// sesion pero no mira el rol, y con eso cualquier encargado con portal podia
// leer las solicitudes de los demas.
[ApiController]
[Route("api/solicitudes")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public sealed class SolicitudesController : ControllerBase
{
    private readonly IServicioSolicitudes _servicio;

    public SolicitudesController(IServicioSolicitudes servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SolicitudResponse>>> ConsultarSolicitudes(
        CancellationToken cancelacion)
        => Ok(await _servicio.ListarAsync(cancelacion));

    // 404 y no 400: la peticion estaba bien armada, lo que no hay es la
    // solicitud. Es el codigo que ya devolvia antes y el que la Web espera.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudDetalleResponse>> ObtenerSolicitud(
        int id, CancellationToken cancelacion)
    {
        var solicitud = await _servicio.ObtenerAsync(id, cancelacion);

        if (solicitud is null)
        {
            return NotFound(new { mensaje = "La solicitud no existe." });
        }

        return Ok(solicitud);
    }

    // Lo consulta la burbuja del menu en todas las pantallas del panel
    [HttpGet("pendientes")]
    public async Task<ActionResult<object>> ContarPendientes(CancellationToken cancelacion)
        => Ok(new { pendientes = await _servicio.ContarPendientesAsync(cancelacion) });

    [HttpGet("estados")]
    public async Task<ActionResult<IReadOnlyList<EstadoSolicitudResponse>>> ListarEstados(
        CancellationToken cancelacion)
        => Ok(await _servicio.ListarEstadosAsync(cancelacion));

    // =====================================================
    // ESCRITURAS
    // =====================================================

    // El modelo lo valida [ApiController] antes de entrar: lo que no pase las
    // anotaciones sale como 400 con { mensaje }, que es lo que la Web ya lee.
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<object>> CambiarEstado(
        int id, [FromBody] CambiarEstadoSolicitudRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.CambiarEstadoAsync(
            User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Solicitud actualizada correctamente." });
    }

    [HttpPut("{id:int}/vincular")]
    public async Task<ActionResult<object>> VincularCliente(
        int id, [FromBody] VincularSolicitudRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.VincularClienteAsync(
            User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Solicitud vinculada al cliente." });
    }
}
