using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Que roles del personal ven la bandeja de mensajes del consultorio (HU-M10-4b).
//
// Esta aparte de ConfiguracionController por lo mismo que MonedaController: ese
// controlador es solo del Administrador y los [Authorize] se suman, no se
// relevan. Este permiso lo cambian el Administrador y la Psicopedagoga (D-2).
// El procedimiento vuelve a validar el rol de quien guarda.
[ApiController]
[Route("api/configuracion")]
[Authorize(Roles = "Administrador,Psicopedagoga")]
public sealed class MensajeriaAccesoController : ControllerBase
{
    private readonly IServicioMensajes _servicio;

    public MensajeriaAccesoController(IServicioMensajes servicio)
    {
        _servicio = servicio;
    }

    // Los tres roles del personal con su marca de acceso
    [HttpGet("mensajeria")]
    public async Task<ActionResult<IReadOnlyList<AccesoMensajeriaRolResponse>>> ListarAcceso(
        CancellationToken cancelacion)
        => Ok(await _servicio.ListarAccesoAsync(cancelacion));

    // Recibe la lista completa de roles que quedan con acceso, ej. { "roles": [2, 3] }
    [HttpPut("mensajeria")]
    public async Task<IActionResult> GuardarAcceso(
        [FromBody] GuardarAccesoMensajeriaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.GuardarAccesoAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { mensaje = "Acceso a los mensajes guardado correctamente." });
    }
}
