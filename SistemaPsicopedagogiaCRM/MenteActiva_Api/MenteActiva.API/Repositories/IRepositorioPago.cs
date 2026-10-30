using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioPago
{
    Task<IReadOnlyList<PagoListaResponse>> ListarAsync(DateTime hoy, CancellationToken cancelacion);

    Task<PagoDetalleResponse> ObtenerAsync(int idPago, CancellationToken cancelacion);

    Task<IReadOnlyList<EncargadoPagadorResponse>> ListarEncargadosAsync(int idEstudiante, CancellationToken cancelacion);

    Task<IReadOnlyList<MetodoPagoResponse>> ListarMetodosAsync(CancellationToken cancelacion);

    Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion);

    Task<int> RegistrarAsync(int idUsuarioAccion, RegistrarPagoRequest peticion, DateTime hoy, CancellationToken cancelacion);

    Task<int> RegistrarAbonoAsync(
        int idUsuarioAccion, int idPago, RegistrarAbonoRequest peticion, DateTime fechaAbono,
        CancellationToken cancelacion);

    Task AnularAbonoAsync(int idUsuarioAccion, int idAbono, string motivo, CancellationToken cancelacion);

    Task AnularAsync(int idUsuarioAccion, int idPago, string motivo, CancellationToken cancelacion);
}