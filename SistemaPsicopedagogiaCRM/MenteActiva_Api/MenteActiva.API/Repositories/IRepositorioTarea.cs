using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioTarea
{
    Task<IReadOnlyList<TareaResponse>> ListarAsync(CancellationToken cancelacion);

    // Nulo cuando no existe o esta dada de baja (el procedimiento avisa con
    // 45000 y aqui se traduce a nulo para que el controlador responda 404).
    Task<TareaDetalleResponse?> ObtenerAsync(int idTarea, CancellationToken cancelacion);

    // Devuelve el id de la tarea creada
    Task<int> CrearAsync(
        int idUsuarioAccion, int? idMaterial, string titulo, string? descripcion,
        DateTime fechaLimite, string estudiantesJson, CancellationToken cancelacion);

    // estudiantesJson nulo deja la asignacion como esta
    Task EditarAsync(
        int idUsuarioAccion, int idTarea, int? idMaterial, string titulo, string? descripcion,
        DateTime fechaLimite, string? estudiantesJson, CancellationToken cancelacion);

    Task EliminarAsync(int idUsuarioAccion, int idTarea, CancellationToken cancelacion);
}
