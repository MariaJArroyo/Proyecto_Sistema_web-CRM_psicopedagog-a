using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Solo traduce HTTP <-> servicio. Las reglas estan en ServicioCitas y los
// errores los convierte ManejadorExcepciones.
[ApiController]
[Route("api/citas")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public sealed class CitasController : ControllerBase
{
    private readonly IServicioCitas _servicio;

    public CitasController(IServicioCitas servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================


    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CitaAgendaResponse>>> ConsultarAgenda(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancelacion)
        => Ok(await _servicio.ConsultarAgendaAsync(desde, hasta, cancelacion));

    [HttpGet("estudiantes")]
    public async Task<ActionResult<IReadOnlyList<EstudianteBusquedaResponse>>> BuscarEstudiantes(
        [FromQuery] string? texto, CancellationToken cancelacion)
        => Ok(await _servicio.BuscarEstudiantesAsync(texto, cancelacion));

    [HttpGet("disponibilidad")]
    public async Task<ActionResult<IReadOnlyList<EspacioDisponibleResponse>>> ConsultarDisponibilidad(
        [FromQuery] DateOnly fecha, [FromQuery] int idTipoSesion,
        [FromQuery] int? idCitaExcluir, [FromQuery] int? idGrupoExcluir,
        CancellationToken cancelacion)
        => Ok(await _servicio.ConsultarDisponibilidadAsync(
            fecha, idTipoSesion, idCitaExcluir, idGrupoExcluir, cancelacion));

    [HttpGet("catalogos")]
    public async Task<ActionResult<CatalogosAgendaResponse>> ObtenerCatalogos(CancellationToken cancelacion)
        => Ok(await _servicio.ObtenerCatalogosAsync(cancelacion));

    // =====================================================
    // CITAS INDIVIDUALES
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCitaRequest peticion, CancellationToken cancelacion)
    {
        var idCita = await _servicio.CrearAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idCita, mensaje = "Cita agendada correctamente." });
    }

    [HttpPut("{id:int}/reprogramar")]
    public async Task<IActionResult> Reprogramar(
        int id, [FromBody] ReprogramarCitaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.ReprogramarAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Cita reprogramada correctamente." });
    }

    [HttpPut("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(
        int id, [FromBody] CancelarCitaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.CancelarAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Cita cancelada." });
    }

    [HttpPut("{id:int}/no-asistio")]
    public async Task<IActionResult> MarcarNoAsistio(int id, CancellationToken cancelacion)
    {
        await _servicio.MarcarNoAsistioAsync(User.ObtenerIdUsuario(), id, cancelacion);

        return Ok(new { mensaje = "La cita quedó marcada como no asistió." });
    }

    // =====================================================
    // GRUPOS
    // =====================================================


    [HttpGet("grupos/disponibles")]
    public async Task<ActionResult<IReadOnlyList<GrupoDisponibleResponse>>> ConsultarGruposDisponibles(
        [FromQuery] DateOnly fecha, CancellationToken cancelacion)
        => Ok(await _servicio.ConsultarGruposDisponiblesAsync(fecha, cancelacion));

    [HttpPost("grupos")]
    public async Task<IActionResult> CrearGrupo([FromBody] CrearGrupoRequest peticion, CancellationToken cancelacion)
    {
        var idGrupoCita = await _servicio.CrearGrupoAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idGrupoCita, mensaje = "Grupo agendado correctamente." });
    }

    [HttpPost("grupos/{id:int}/estudiantes")]
    public async Task<IActionResult> AgregarEstudianteGrupo(
        int id, [FromBody] AgregarEstudianteGrupoRequest peticion, CancellationToken cancelacion)
    {
        var idCita = await _servicio.AgregarEstudianteGrupoAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { idCita, mensaje = "Estudiante agregado al grupo." });
    }

    [HttpPut("grupos/{id:int}")]
    public async Task<IActionResult> EditarGrupo(
        int id, [FromBody] EditarGrupoRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.EditarGrupoAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Grupo actualizado." });
    }

    [HttpPut("grupos/{id:int}/reprogramar")]
    public async Task<IActionResult> ReprogramarGrupo(
        int id, [FromBody] ReprogramarCitaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.ReprogramarGrupoAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Grupo reprogramado correctamente." });
    }

    [HttpPut("grupos/{id:int}/cancelar")]
    public async Task<IActionResult> CancelarGrupo(
        int id, [FromBody] CancelarCitaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.CancelarGrupoAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Grupo cancelado." });
    }
}