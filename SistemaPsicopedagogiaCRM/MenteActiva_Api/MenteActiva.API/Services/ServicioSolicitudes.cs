using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Las reglas de la solicitud viven en los procedimientos, que es el unico lugar
// por donde se escribe: que exista, que no este ya convertida, que el estado sea
// uno de los permitidos, que el cliente a vincular exista. Todas salen como
// SIGNAL 45000 y llegan aqui ya convertidas en ReglaNegocioException.
//
// Lo que se queda en esta capa es el guardia del id, que no es una regla de
// negocio sino evitar un viaje a la base con un valor que no puede existir.
public sealed class ServicioSolicitudes : IServicioSolicitudes
{
    private readonly IRepositorioSolicitud _repositorio;

    public ServicioSolicitudes(IRepositorioSolicitud repositorio)
    {
        _repositorio = repositorio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    public Task<IReadOnlyList<SolicitudResponse>> ListarAsync(CancellationToken cancelacion)
        => _repositorio.ListarAsync(cancelacion);

    public Task<SolicitudDetalleResponse?> ObtenerAsync(
        int idSolicitud, CancellationToken cancelacion)
    {
        if (idSolicitud <= 0)
        {
            // Mismo resultado que un id que no existe: el controlador responde 404
            return Task.FromResult<SolicitudDetalleResponse?>(null);
        }

        return _repositorio.ObtenerAsync(idSolicitud, cancelacion);
    }

    public Task<int> ContarPendientesAsync(CancellationToken cancelacion)
        => _repositorio.ContarPendientesAsync(cancelacion);

    public Task<IReadOnlyList<EstadoSolicitudResponse>> ListarEstadosAsync(
        CancellationToken cancelacion)
        => _repositorio.ListarEstadosAsync(cancelacion);

    // =====================================================
    // ESCRITURAS
    // =====================================================

    public Task CambiarEstadoAsync(
        int idUsuarioAccion, int idSolicitud, CambiarEstadoSolicitudRequest peticion,
        CancellationToken cancelacion)
    {
        ValidarId(idSolicitud);

        return _repositorio.CambiarEstadoAsync(
            idUsuarioAccion, idSolicitud, peticion, cancelacion);
    }

    public Task VincularClienteAsync(
        int idUsuarioAccion, int idSolicitud, VincularSolicitudRequest peticion,
        CancellationToken cancelacion)
    {
        ValidarId(idSolicitud);

        return _repositorio.VincularClienteAsync(
            idUsuarioAccion, idSolicitud, peticion.IdEncargado, cancelacion);
    }

    private static void ValidarId(int idSolicitud)
    {
        if (idSolicitud <= 0)
        {
            throw new ReglaNegocioException("La solicitud indicada no es válida.");
        }
    }
}
