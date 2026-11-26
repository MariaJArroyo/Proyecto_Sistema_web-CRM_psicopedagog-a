using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Repositories;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Foto del estudiante en la ficha. Verla: los tres roles del panel (igual que
// la ficha). Cambiarla o quitarla: Administrador y Psicopedagoga.
[ApiController]
[Route("api/estudiantes/{idEstudiante:int}/foto")]
public sealed class FotosEstudianteController : ControllerBase
{
    private readonly IRepositorioFotoEstudiante _repositorio;
    private readonly IAlmacenFotosEstudiante _almacen;

    public FotosEstudianteController(IRepositorioFotoEstudiante repositorio, IAlmacenFotosEstudiante almacen)
    {
        _repositorio = repositorio;
        _almacen = almacen;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
    public async Task<IActionResult> Ver(int idEstudiante, CancellationToken cancelacion)
    {
        var foto = await _repositorio.ObtenerAsync(idEstudiante, cancelacion);
        var flujo = foto is null ? null : _almacen.Abrir(foto.FotoArchivo);

        if (flujo is null)
        {
            return NotFound(new { mensaje = "El estudiante no tiene foto." });
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(flujo, string.IsNullOrWhiteSpace(foto!.FotoTipo) ? "image/jpeg" : foto.FotoTipo);
    }

    // multipart/form-data con el campo "foto"
    [HttpPost]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    [Consumes("multipart/form-data")]
    [TypeFilter(typeof(LimiteSubidaFotoEstudiante))]
    public async Task<IActionResult> Guardar(int idEstudiante, IFormFile? foto, CancellationToken cancelacion)
    {
        if (foto is null || foto.Length == 0)
        {
            throw new ReglaNegocioException("Elija una foto.");
        }

        if (foto.Length > AlmacenFotosEstudiante.TamanoMaximoBytes)
        {
            throw new ReglaNegocioException(
                $"La foto no puede pasar de {AlmacenFotosEstudiante.TamanoMaximoMb} MB.");
        }

        string nombre;
        string tipo;

        await using (var flujo = foto.OpenReadStream())
        {
            (nombre, tipo) = await _almacen.GuardarAsync(flujo, cancelacion);
        }

        string? anterior;

        try
        {
            anterior = await _repositorio.GuardarAsync(idEstudiante, nombre, tipo, cancelacion);
        }
        catch
        {
            // Si la base no la acepta, el archivo nuevo no se queda huerfano
            _almacen.Borrar(nombre);
            throw;
        }

        _almacen.Borrar(anterior);

        return Ok(new { mensaje = "Foto actualizada.", version = DateTime.Now.Ticks });
    }

    [HttpDelete]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> Quitar(int idEstudiante, CancellationToken cancelacion)
    {
        var anterior = await _repositorio.GuardarAsync(idEstudiante, null, null, cancelacion);
        _almacen.Borrar(anterior);

        return Ok(new { mensaje = "Foto eliminada." });
    }
}
