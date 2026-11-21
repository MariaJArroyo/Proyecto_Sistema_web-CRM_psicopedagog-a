using System.Text.Json;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Revisa la peticion antes de llamar al procedimiento para devolver un mensaje
// claro. Las reglas de fondo (material activo, al menos un estudiante activo,
// bitacora) las sigue cuidando el procedimiento.
public sealed class ServicioTareas : IServicioTareas
{
    private readonly IRepositorioTarea _repositorio;
    private readonly IRelojNegocio _reloj;

    public ServicioTareas(IRepositorioTarea repositorio, IRelojNegocio reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    public Task<IReadOnlyList<TareaResponse>> ListarAsync(CancellationToken cancelacion)
        => _repositorio.ListarAsync(cancelacion);

    public Task<TareaDetalleResponse?> ObtenerAsync(int idTarea, CancellationToken cancelacion)
    {
        if (idTarea <= 0)
        {
            return Task.FromResult<TareaDetalleResponse?>(null);
        }

        return _repositorio.ObtenerAsync(idTarea, cancelacion);
    }

    public async Task<int> CrearAsync(
        int idUsuarioAccion, CrearTareaRequest peticion, CancellationToken cancelacion)
    {
        var titulo = ValidarTitulo(peticion.Titulo);
        var fecha = ValidarFecha(peticion.FechaLimite);

        if (fecha < _reloj.Hoy)
        {
            throw new ReglaNegocioException("La fecha límite no puede ser anterior a hoy.");
        }

        var estudiantes = ArmarListaEstudiantes(peticion.IdEstudiantes)
            ?? throw new ReglaNegocioException("Asigne la tarea a por lo menos un estudiante.");

        return await _repositorio.CrearAsync(
            idUsuarioAccion, peticion.IdMaterial, titulo, Limpiar(peticion.Descripcion),
            fecha.ToDateTime(TimeOnly.MinValue), estudiantes, cancelacion);
    }

    public async Task EditarAsync(
        int idUsuarioAccion, int idTarea, EditarTareaRequest peticion, CancellationToken cancelacion)
    {
        var titulo = ValidarTitulo(peticion.Titulo);
        var fecha = ValidarFecha(peticion.FechaLimite);

        var actual = await ObtenerAsync(idTarea, cancelacion)
            ?? throw new ReglaNegocioException("La tarea indicada no existe.");

        // Una tarea ya vencida se puede seguir editando (titulo, material...):
        // solo se rechaza el pasado cuando la fecha es la que se esta cambiando.
        if (fecha < _reloj.Hoy && fecha != DateOnly.FromDateTime(actual.FechaLimite))
        {
            throw new ReglaNegocioException("La fecha límite no puede ser anterior a hoy.");
        }

        await _repositorio.EditarAsync(
            idUsuarioAccion, idTarea, peticion.IdMaterial, titulo, Limpiar(peticion.Descripcion),
            fecha.ToDateTime(TimeOnly.MinValue), ArmarListaEstudiantes(peticion.IdEstudiantes),
            cancelacion);
    }

    public Task EliminarAsync(int idUsuarioAccion, int idTarea, CancellationToken cancelacion)
        => _repositorio.EliminarAsync(idUsuarioAccion, idTarea, cancelacion);

    // =====================================================
    // VALIDACIONES
    // =====================================================

    private static string ValidarTitulo(string? titulo)
    {
        var limpio = Limpiar(titulo);

        return limpio ?? throw new ReglaNegocioException("Escriba el título de la tarea.");
    }

    private static DateOnly ValidarFecha(DateTime? fecha)
    {
        if (fecha is null)
        {
            throw new ReglaNegocioException("Indique la fecha límite de la tarea.");
        }

        return DateOnly.FromDateTime(fecha.Value);
    }

    // Nulo cuando no hay estudiantes en la lista. Sin repetidos.
    private static string? ArmarListaEstudiantes(List<int>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return null;
        }

        if (ids.Any(id => id <= 0))
        {
            throw new ReglaNegocioException("La lista de estudiantes no tiene un formato válido.");
        }

        return JsonSerializer.Serialize(ids.Distinct().ToList());
    }

    private static string? Limpiar(string? texto)
    {
        var limpio = texto?.Trim();

        return string.IsNullOrEmpty(limpio) ? null : limpio;
    }
}
