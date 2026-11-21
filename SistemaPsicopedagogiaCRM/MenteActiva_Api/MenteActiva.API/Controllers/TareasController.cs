using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Pestania de tareas del panel (HU-M11-3). Solo traduce HTTP <-> servicio: las
// reglas estan en ServicioTareas y en los procedimientos, y los errores los
// convierte ManejadorExcepciones (las reglas que no se cumplen salen como 400
// con { mensaje }).
//
// Los tres roles internos, como el resto de materiales. Entregar y revisar
// tareas no pasa por aqui. El selector de estudiantes sale de
// GET api/estudiantes, que ya devuelve solo los activos.
[ApiController]
[Route("api/tareas")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public sealed class TareasController : ControllerBase
{
    private readonly IServicioTareas _servicio;

    public TareasController(IServicioTareas servicio)
    {
        _servicio = servicio;
    }

    // Tareas activas con cuantos estudiantes tienen asignada y cuantos entregaron
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TareaResponse>>> Listar(CancellationToken cancelacion)
        => Ok(await _servicio.ListarAsync(cancelacion));

    // La tarea con sus estudiantes asignados, para el formulario de edicion
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TareaDetalleResponse>> Obtener(int id, CancellationToken cancelacion)
    {
        var tarea = await _servicio.ObtenerAsync(id, cancelacion);

        if (tarea is null)
        {
            return NotFound(new { mensaje = "La tarea no existe." });
        }

        return Ok(tarea);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Crear(
        [FromBody] CrearTareaRequest peticion, CancellationToken cancelacion)
    {
        var id = await _servicio.CrearAsync(User.ObtenerIdUsuario(), peticion, cancelacion);

        return Ok(new { idTarea = id, mensaje = "Tarea creada y asignada correctamente." });
    }

    // idEstudiantes vacio o ausente deja la asignacion como esta
    [HttpPut("{id:int}")]
    public async Task<ActionResult<object>> Editar(
        int id, [FromBody] EditarTareaRequest peticion, CancellationToken cancelacion)
    {
        await _servicio.EditarAsync(User.ObtenerIdUsuario(), id, peticion, cancelacion);

        return Ok(new { mensaje = "Tarea actualizada correctamente." });
    }

    // Baja logica
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<object>> Eliminar(int id, CancellationToken cancelacion)
    {
        await _servicio.EliminarAsync(User.ObtenerIdUsuario(), id, cancelacion);

        return Ok(new { mensaje = "Tarea eliminada." });
    }
}
