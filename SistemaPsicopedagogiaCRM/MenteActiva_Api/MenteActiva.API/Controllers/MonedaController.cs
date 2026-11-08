using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// La moneda de TB_CONFIGURACION, y nada mas.
//
// Esta aparte de ConfiguracionController por una razon concreta: ese controlador es
// del Administrador y los dos [Authorize] se suman, no se relevan, asi que una
// accion con [Authorize] adentro seguiria pidiendo el rol de la clase. Y el codigo
// de la moneda lo necesita cualquiera que vea un monto en pantalla, incluido el
// encargado en el portal; sin el, el formato de dinero de la Web tendria que llevar
// el simbolo escrito a mano.
//
// Sigue pidiendo sesion. Lo que devuelve es un codigo ISO de tres letras.
[ApiController]
[Route("api/configuracion")]
[Authorize]
public sealed class MonedaController : ControllerBase
{
    private readonly IServicioConfiguracion _servicio;

    public MonedaController(IServicioConfiguracion servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("moneda")]
    public async Task<ActionResult<MonedaResponse>> ObtenerMoneda(CancellationToken cancelacion)
        => Ok(new MonedaResponse { Moneda = await _servicio.ObtenerMonedaAsync(cancelacion) });
}
