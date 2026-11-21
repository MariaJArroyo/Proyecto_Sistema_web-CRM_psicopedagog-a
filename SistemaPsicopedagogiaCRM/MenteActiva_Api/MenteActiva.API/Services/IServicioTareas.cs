using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioTareas
{
    Task<IReadOnlyList<TareaResponse>> ListarAsync(CancellationToken cancelacion);

    // Nulo cuando no existe o esta dada de baja, para que el controlador
    // responda 404
    Task<TareaDetalleResponse?> ObtenerAsync(int idTarea, CancellationToken cancelacion);

    // Devuelve el id creado. ReglaNegocioException si el titulo, la fecha o la
    // lista de estudiantes no son validos, o si el procedimiento rechaza algo.
    Task<int> CrearAsync(int idUsuarioAccion, CrearTareaRequest peticion, CancellationToken cancelacion);

    Task EditarAsync(
        int idUsuarioAccion, int idTarea, EditarTareaRequest peticion, CancellationToken cancelacion);

    // Baja logica
    Task EliminarAsync(int idUsuarioAccion, int idTarea, CancellationToken cancelacion);
}
