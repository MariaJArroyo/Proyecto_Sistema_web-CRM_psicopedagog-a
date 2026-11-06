using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Los numeros de las burbujas del menu, en un solo lugar porque alimentan un
// solo componente de la pantalla.
//
// El rol va por accion y no en la clase a proposito: cada burbuja pide el mismo
// rol que la pantalla que abre, para que nadie vea la cuenta de algo que no
// puede entrar a ver.
[ApiController]
[Route("api/contadores")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class ContadoresController : ControllerBase
{
    private const string RolesFicha = "Administrador,Psicopedagoga";

    private readonly IServicioContadores _servicio;

    public ContadoresController(IServicioContadores servicio)
    {
        _servicio = servicio;
    }

    // La pantalla de "Acceso al portal" es de administrador y psicopedagoga
    [HttpGet("encargados-sin-invitar")]
    [Authorize(Roles = RolesFicha)]
    public async Task<ActionResult<object>> EncargadosSinInvitar(CancellationToken cancelacion)
        => Ok(new { total = await _servicio.EncargadosSinInvitarAsync(cancelacion) });

    [HttpGet("notificaciones-fallidas")]
    public async Task<ActionResult<object>> NotificacionesFallidas(CancellationToken cancelacion)
        => Ok(new { total = await _servicio.NotificacionesFallidasAsync(cancelacion) });

    // La bitacora de citas lleva la nota clinica del menor: fuera el asistente,
    // igual que en SesionesController
    [HttpGet("citas-sin-sesion")]
    [Authorize(Roles = RolesFicha)]
    public async Task<ActionResult<object>> CitasSinSesion(CancellationToken cancelacion)
        => Ok(new { total = await _servicio.CitasSinSesionAsync(cancelacion) });

    [HttpGet("pagos-vencidos")]
    public async Task<ActionResult<object>> PagosVencidos(CancellationToken cancelacion)
        => Ok(new { total = await _servicio.PagosVencidosAsync(cancelacion) });
}
