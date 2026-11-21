using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Pestanias de materiales del panel. Solo traduce HTTP <-> servicio: las reglas
// estan en ServicioMateriales y los errores los convierte ManejadorExcepciones
// (las reglas que no se cumplen salen como 400 con { mensaje }).
//
// Antes abria su propia conexion y llamaba a SP_ConsultarMateriales, que no
// existe, asi que el listado respondia 500.
//
// Los tres roles internos, como la pantalla. El portal lee lo publicado por
// PortalController, no por aqui.
[ApiController]
[Route("api/materiales")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public sealed class MaterialesController : ControllerBase
{
    private readonly IServicioMateriales _servicio;

    public MaterialesController(IServicioMateriales servicio)
    {
        _servicio = servicio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    // idTipoMaterial: 1 Guia, 2 PDF, 3 Enlace, 4 Actividad. Sin filtros trae
    // todo lo activo, borradores incluidos.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialResponse>>> Listar(
        [FromQuery] int? idTipoMaterial, [FromQuery] bool? soloPublicados,
        [FromQuery] string? busqueda, CancellationToken cancelacion)
        => Ok(await _servicio.ListarAsync(idTipoMaterial, soloPublicados, busqueda, cancelacion));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MaterialResponse>> Obtener(int id, CancellationToken cancelacion)
    {
        var material = await _servicio.ObtenerAsync(id, cancelacion);

        if (material is null)
        {
            return NotFound(new { mensaje = "El material no existe." });
        }

        return Ok(material);
    }

    // El PDF de un material, aunque sea borrador. Se descarga con el titulo
    // como nombre; el nombre en disco no sale del API.
    [HttpGet("{id:int}/archivo")]
    public async Task<IActionResult> DescargarArchivo(int id, CancellationToken cancelacion)
    {
        var archivo = await _servicio.ObtenerArchivoAsync(id, cancelacion);

        if (archivo is null)
        {
            return NotFound(new { mensaje = "El archivo no est\u00e1 disponible." });
        }

        Response.Headers.CacheControl = "no-store";

        return File(archivo.Contenido, "application/pdf", archivo.NombreDescarga);
    }

    // Responde 200 tambien cuando el enlace esta caido: { disponible, mensaje }
    [HttpGet("{id:int}/verificar-enlace")]
    public async Task<ActionResult<VerificacionEnlaceResponse>> VerificarEnlace(
        int id, CancellationToken cancelacion)
    {
        var resultado = await _servicio.VerificarEnlaceAsync(id, cancelacion);

        if (resultado is null)
        {
            return NotFound(new { mensaje = "El material no existe." });
        }

        return Ok(resultado);
    }

    // =====================================================
    // ALTAS
    // =====================================================

    // multipart/form-data con los campos archivo, titulo (opcional: si no
    // viene, el nombre del archivo), descripcion, idNivelEducativo y publicar.
    // El tope de la peticion lo pone LimiteSubidaPdf, solo aqui.
    [HttpPost("pdf")]
    [Consumes("multipart/form-data")]
    [TypeFilter(typeof(LimiteSubidaPdf))]
    public async Task<ActionResult<object>> CrearPdf(
        [FromForm] CrearPdfRequest peticion, CancellationToken cancelacion)
    {
        var id = await _servicio.CrearPdfAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idMaterial = id, mensaje = "Archivo cargado correctamente." });
    }

    [HttpPost("guias")]
    public async Task<ActionResult<object>> CrearGuia(
        [FromBody] CrearGuiaRequest peticion, CancellationToken cancelacion)
    {
        var id = await _servicio.CrearGuiaAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idMaterial = id, mensaje = "Material registrado correctamente." });
    }

    [HttpPost("enlaces")]
    public async Task<ActionResult<object>> CrearEnlace(
        [FromBody] CrearEnlaceRequest peticion, CancellationToken cancelacion)
    {
        var id = await _servicio.CrearEnlaceAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idMaterial = id, mensaje = "Material registrado correctamente." });
    }

    [HttpPost("actividades")]
    public async Task<ActionResult<object>> CrearActividad(
        [FromBody] CrearActividadRequest peticion, CancellationToken cancelacion)
    {
        var id = await _servicio.CrearActividadAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idMaterial = id, mensaje = "Material registrado correctamente." });
    }

    // =====================================================
    // CAMBIOS
    // =====================================================

    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> Editar(
        int id, [FromBody] EditarMaterialRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.EditarAsync(id, peticion, cancelacion);

        return Ok(new { mensaje = "Material actualizado correctamente." });
    }

    [HttpPut("{id:int}/publicacion")]
    public async Task<ActionResult<object>> CambiarPublicacion(
        int id, [FromBody] PublicacionMaterialRequest peticion, CancellationToken cancelacion)
    {
        var publicado = peticion.Publicado!.Value;

        await _servicio.CambiarPublicacionAsync(id, publicado, cancelacion);

        return Ok(new
        {
            mensaje = publicado ? "Material publicado." : "Material retirado del portal."
        });
    }

    // Baja logica
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> Eliminar(int id, CancellationToken cancelacion)
    {
        await _servicio.EliminarAsync(id, cancelacion);

        return Ok(new { mensaje = "Material eliminado." });
    }
}
