using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Solo traduce HTTP <-> servicio. Las reglas estan en ServicioPagos y en los
// procedimientos; los errores los convierte ManejadorExcepciones.
// Incluye al Asistente: es apoyo administrativo en agenda, clientes y pagos.
[ApiController]
[Route("api/pagos")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public sealed class PagosController : ControllerBase
{
    private readonly IServicioPagos _servicio;

    public PagosController(IServicioPagos servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    [HttpGet]
    public async Task<ActionResult<PagosPantallaResponse>> Listar(CancellationToken cancelacion)
        => Ok(await _servicio.ListarAsync(cancelacion));

    [HttpGet("catalogos")]
    public async Task<ActionResult<CatalogosPagoResponse>> ObtenerCatalogos(CancellationToken cancelacion)
        => Ok(await _servicio.ObtenerCatalogosAsync(cancelacion));

    [HttpGet("{idPago:int}")]
    public async Task<ActionResult<PagoDetalleResponse>> Obtener(int idPago, CancellationToken cancelacion)
        => Ok(await _servicio.ObtenerAsync(idPago, cancelacion));

    [HttpGet("estudiantes/{idEstudiante:int}/encargados")]
    public async Task<ActionResult<IReadOnlyList<EncargadoPagadorResponse>>> ListarEncargados(
        int idEstudiante, CancellationToken cancelacion)
        => Ok(await _servicio.ListarEncargadosAsync(idEstudiante, cancelacion));

    // =====================================================
    // ACCIONES
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPagoRequest peticion, CancellationToken cancelacion)
    {
        var idPago = await _servicio.RegistrarAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new
        {
            idPago,
            mensaje = peticion.YaPagado ? "Pago registrado correctamente." : "Cobro pendiente registrado."
        });
    }

    [HttpPost("{idPago:int}/abonos")]
    public async Task<IActionResult> RegistrarAbono(
        int idPago, [FromBody] RegistrarAbonoRequest peticion, CancellationToken cancelacion)
    {
        var idAbono = await _servicio.RegistrarAbonoAsync(User.ObtenerIdUsuario(), idPago, peticion, cancelacion);

        return Ok(new { idAbono, mensaje = "Pago registrado correctamente." });
    }

    [HttpPut("abonos/{idAbono:int}/anular")]
    public async Task<IActionResult> AnularAbono(
        int idAbono, [FromBody] AnularPagoRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.AnularAbonoAsync(User.ObtenerIdUsuario(), idAbono, peticion, cancelacion);

        return Ok(new { mensaje = "Pago anulado. El saldo del cobro se recalculó." });
    }

    [HttpPut("{idPago:int}/anular")]
    public async Task<IActionResult> Anular(
        int idPago, [FromBody] AnularPagoRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.AnularAsync(User.ObtenerIdUsuario(), idPago, peticion, cancelacion);

        return Ok(new { mensaje = "Cobro anulado." });
    }
}