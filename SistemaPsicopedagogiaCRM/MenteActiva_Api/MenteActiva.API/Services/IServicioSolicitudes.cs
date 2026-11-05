using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioSolicitudes
{
    Task<IReadOnlyList<SolicitudResponse>> ListarAsync(CancellationToken cancelacion);

    // Nulo cuando no existe, para que el controlador pueda responder 404 y no
    // un 400 que diria que la peticion venia mal armada
    Task<SolicitudDetalleResponse?> ObtenerAsync(int idSolicitud, CancellationToken cancelacion);

    Task<int> ContarPendientesAsync(CancellationToken cancelacion);

    Task<IReadOnlyList<EstadoSolicitudResponse>> ListarEstadosAsync(CancellationToken cancelacion);

    Task CambiarEstadoAsync(
        int idUsuarioAccion, int idSolicitud, CambiarEstadoSolicitudRequest peticion,
        CancellationToken cancelacion);

    Task VincularClienteAsync(
        int idUsuarioAccion, int idSolicitud, VincularSolicitudRequest peticion,
        CancellationToken cancelacion);
}
