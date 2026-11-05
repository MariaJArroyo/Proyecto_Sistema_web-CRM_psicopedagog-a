using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioSolicitud
{
    // ---------- consultas ----------

    Task<IReadOnlyList<SolicitudResponse>> ListarAsync(CancellationToken cancelacion);

    // Nulo cuando la solicitud no existe. Trae ya cruzados los dos resultados
    // del procedimiento: la solicitud y los clientes que se le parecen.
    Task<SolicitudDetalleResponse?> ObtenerAsync(int idSolicitud, CancellationToken cancelacion);

    // Para el contador del menu del panel
    Task<int> ContarPendientesAsync(CancellationToken cancelacion);

    Task<IReadOnlyList<EstadoSolicitudResponse>> ListarEstadosAsync(CancellationToken cancelacion);

    // ---------- escrituras ----------

    // Pendiente, Contactada o Descartada, con su nota interna. "Convertida" no
    // se pone por aqui: a ese estado solo se llega convirtiendo o vinculando.
    Task CambiarEstadoAsync(
        int idUsuarioAccion, int idSolicitud, CambiarEstadoSolicitudRequest peticion,
        CancellationToken cancelacion);

    // Enlaza la solicitud a un encargado que ya existia, para el caso del
    // duplicado. El procedimiento le pega el mensaje a sus observaciones.
    Task VincularClienteAsync(
        int idUsuarioAccion, int idSolicitud, int idEncargado, CancellationToken cancelacion);
}
