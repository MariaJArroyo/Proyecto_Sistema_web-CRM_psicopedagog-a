using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Datos de solo lectura que la agenda necesita para dibujarse y que
// cualquier rol del CRM puede ver (la edicion vive en ConfiguracionController).
[ApiController]
[Route("api/agenda")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public sealed class AgendaController : ControllerBase
{
    private readonly IServicioConfiguracion _servicio;

    public AgendaController(IServicioConfiguracion servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("dias-no-laborales")]
    public async Task<ActionResult<IReadOnlyList<DiaNoLaboralResponse>>> ListarDiasNoLaborales(
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancelacion)
        => Ok(await _servicio.ListarDiasNoLaboralesAsync(desde, hasta, cancelacion));
}