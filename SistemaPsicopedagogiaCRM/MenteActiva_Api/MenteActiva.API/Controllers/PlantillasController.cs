using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Solo traduce HTTP <-> servicio. Las reglas estan en ServicioPlantillas y en
// los procedimientos; los errores los convierte ManejadorExcepciones.
//
// Leer lo pueden los tres roles del panel, porque el Asistente tambien escribe
// mensajes a los encargados y necesita las plantillas para eso. Administrarlas
// queda en Administrador y Psicopedagoga: el texto de una plantilla sale a
// nombre del consultorio y lo recibe una familia.
[ApiController]
[Route("api/plantillas")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public sealed class PlantillasController : ControllerBase
{
    private const string RolesAdministracion = "Administrador,Psicopedagoga";

    private readonly IServicioPlantillas _servicio;

    public PlantillasController(IServicioPlantillas servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    // soloActivas=true es lo que pide el selector al redactar un mensaje
    // (HU-M10-5, escenario 1); la pantalla de administracion las pide todas.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlantillaResponse>>> Listar(
        [FromQuery] bool soloActivas = false, CancellationToken cancelacion = default)
        => Ok(await _servicio.ListarAsync(soloActivas, cancelacion));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlantillaResponse>> ObtenerDetalle(
        int id, CancellationToken cancelacion)
        => Ok(await _servicio.ObtenerAsync(id, cancelacion));

    // La lista de marcadores sale del API para que la pantalla no la repita y se
    // desincronice con la que valida el servidor.
    [HttpGet("catalogos")]
    public ActionResult<CatalogosPlantillaResponse> ObtenerCatalogos()
        => Ok(_servicio.ObtenerCatalogos());

    // =====================================================
    // ESCRITURAS
    // =====================================================

    [HttpPost]
    [Authorize(Roles = RolesAdministracion)]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarPlantillaRequest peticion, CancellationToken cancelacion)
    {
        var idPlantilla = await _servicio.CrearAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idPlantilla, mensaje = "Plantilla creada correctamente." });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesAdministracion)]
    public async Task<IActionResult> Editar(
        int id, [FromBody] GuardarPlantillaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.EditarAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Plantilla actualizada correctamente." });
    }

    // Baja logica. No hay DELETE a proposito: una plantilla usada en un correo
    // que ya salio no se desaparece.
    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = RolesAdministracion)]
    public async Task<IActionResult> CambiarEstado(
        int id, [FromBody] CambiarEstadoPlantillaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.CambiarEstadoAsync(
            User.ObtenerIdUsuario(), id, peticion.Activa!.Value, cancelacion);

        return Ok(new
        {
            mensaje = peticion.Activa!.Value
                ? "Plantilla activada correctamente."
                : "Plantilla desactivada correctamente."
        });
    }
}
