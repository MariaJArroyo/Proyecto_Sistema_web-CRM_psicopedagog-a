using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Solo traduce HTTP <-> servicio. Las reglas estan en ServicioSesiones y en los
// procedimientos; los errores los convierte ManejadorExcepciones.
//
// Sin el rol Asistente, a diferencia de la agenda: una sesion lleva la nota
// clinica del menor (tema trabajado, avances, observaciones) y el Asistente es
// apoyo administrativo en agenda, clientes y pagos.
[ApiController]
[Route("api/sesiones")]
[Authorize(Roles = "Administrador,Psicopedagoga")]
public sealed class SesionesController : ControllerBase
{
    private readonly IServicioSesiones _servicio;

    public SesionesController(IServicioSesiones servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    // Historial. Sin filtros trae todas las activas; con idEstudiante, las de
    // ese estudiante (HU-M5-6). La lista vacia es respuesta valida: la pantalla
    // muestra el mensaje de "no hay sesiones registradas".
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SesionListaResponse>>> Listar(
        [FromQuery] int? idEstudiante, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        CancellationToken cancelacion)
        => Ok(await _servicio.ListarAsync(idEstudiante, desde, hasta, cancelacion));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SesionDetalleResponse>> ObtenerDetalle(
        int id, CancellationToken cancelacion)
        => Ok(await _servicio.ObtenerAsync(id, cancelacion));

    [HttpGet("catalogos")]
    public async Task<ActionResult<CatalogosSesionResponse>> ObtenerCatalogos(CancellationToken cancelacion)
        => Ok(await _servicio.ObtenerCatalogosAsync(cancelacion));

    [HttpGet("estudiantes")]
    public async Task<ActionResult<IReadOnlyList<EstudianteBusquedaResponse>>> BuscarEstudiantes(
        [FromQuery] string? texto, CancellationToken cancelacion)
        => Ok(await _servicio.BuscarEstudiantesAsync(texto, cancelacion));

    [HttpGet("citas-disponibles")]
    public async Task<ActionResult<IReadOnlyList<CitaDisponibleSesionResponse>>> ListarCitasDisponibles(
        [FromQuery] int idEstudiante, CancellationToken cancelacion)
        => Ok(await _servicio.ListarCitasDisponiblesAsync(idEstudiante, cancelacion));

    // =====================================================
    // ESCRITURAS
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarSesionRequest peticion, CancellationToken cancelacion)
    {
        var idSesion = await _servicio.RegistrarAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idSesion, mensaje = "Sesión registrada correctamente." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(
        int id, [FromBody] EditarSesionRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.EditarAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Sesión actualizada correctamente." });
    }

    // Baja logica. No hay DELETE a proposito: la sesion nunca se borra.
    [HttpPut("{id:int}/inactivar")]
    public async Task<IActionResult> Inactivar(int id, CancellationToken cancelacion)
    {
        await _servicio.InactivarAsync(User.ObtenerIdUsuario(), id, cancelacion);

        return Ok(new { mensaje = "Sesión inactivada correctamente." });
    }
}
