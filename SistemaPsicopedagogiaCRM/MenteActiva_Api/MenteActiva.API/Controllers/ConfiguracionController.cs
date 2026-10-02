using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Configuracion del consultorio: solo el Administrador.
// Solo traduce HTTP <-> servicio; los errores los convierte ManejadorExcepciones.
[ApiController]
[Route("api/configuracion")]
[Authorize(Roles = "Administrador")]
public sealed class ConfiguracionController : ControllerBase
{
    private readonly IServicioConfiguracion _servicio;

    public ConfiguracionController(IServicioConfiguracion servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // HORARIO DE ATENCION
    // =====================================================

    [HttpGet("horario")]
    public async Task<ActionResult<IReadOnlyList<FranjaHorarioResponse>>> ListarHorario(CancellationToken cancelacion)
        => Ok(await _servicio.ListarHorarioAsync(cancelacion));

    [HttpPut("horario")]
    public async Task<ActionResult<GuardarHorarioResponse>> GuardarHorario(
        [FromBody] GuardarHorarioRequest peticion, CancellationToken cancelacion)
        => Ok(await _servicio.GuardarHorarioAsync(User.ObtenerIdUsuario(), peticion, cancelacion));

    // =====================================================
    // DIAS NO LABORALES
    // =====================================================

    [HttpGet("dias-no-laborales")]
    public async Task<ActionResult<IReadOnlyList<DiaNoLaboralResponse>>> ListarDiasNoLaborales(
        CancellationToken cancelacion)
        => Ok(await _servicio.ListarDiasNoLaboralesAsync(cancelacion));

    [HttpPost("dias-no-laborales")]
    public async Task<IActionResult> AgregarDiaNoLaboral(
        [FromBody] AgregarDiaNoLaboralRequest peticion, CancellationToken cancelacion)
    {
        var idDiaNoLaboral = await _servicio.AgregarDiaNoLaboralAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idDiaNoLaboral, mensaje = "Día no laboral agregado. La agenda queda bloqueada esa fecha." });
    }

    [HttpDelete("dias-no-laborales/{idDiaNoLaboral:int}")]
    public async Task<IActionResult> EliminarDiaNoLaboral(int idDiaNoLaboral, CancellationToken cancelacion)
    {
        await _servicio.EliminarDiaNoLaboralAsync(User.ObtenerIdUsuario(), idDiaNoLaboral, cancelacion);

        return Ok(new { mensaje = "Día no laboral eliminado. La fecha vuelve a estar disponible." });
    }

    // =====================================================
    // TIPOS DE SESION
    // =====================================================

    [HttpGet("tipos-sesion")]
    public async Task<ActionResult<IReadOnlyList<TipoSesionResponse>>> ListarTiposSesion(CancellationToken cancelacion)
        => Ok(await _servicio.ListarTiposSesionAsync(cancelacion));

    [HttpPost("tipos-sesion")]
    public async Task<IActionResult> CrearTipoSesion(
        [FromBody] GuardarTipoSesionRequest peticion, CancellationToken cancelacion)
    {
        var idTipoSesion = await _servicio.CrearTipoSesionAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idTipoSesion, mensaje = "Tipo de sesión creado correctamente." });
    }

    [HttpPut("tipos-sesion/{idTipoSesion:int}")]
    public async Task<IActionResult> EditarTipoSesion(
        int idTipoSesion, [FromBody] GuardarTipoSesionRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.EditarTipoSesionAsync(User.ObtenerIdUsuario(), idTipoSesion, peticion, cancelacion);

        return Ok(new { mensaje = "Tipo de sesión actualizado. La nueva duración aplica a las citas nuevas." });
    }

    // =====================================================
    // DATOS DEL CONSULTORIO
    // =====================================================

    [HttpGet("consultorio")]
    public async Task<ActionResult<DatosConsultorioResponse>> ObtenerDatosConsultorio(CancellationToken cancelacion)
        => Ok(await _servicio.ObtenerDatosConsultorioAsync(cancelacion));

    [HttpPut("consultorio")]
    public async Task<IActionResult> GuardarDatosConsultorio(
        [FromBody] GuardarDatosConsultorioRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.GuardarDatosConsultorioAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { mensaje = "Datos del consultorio guardados correctamente." });
    }
}