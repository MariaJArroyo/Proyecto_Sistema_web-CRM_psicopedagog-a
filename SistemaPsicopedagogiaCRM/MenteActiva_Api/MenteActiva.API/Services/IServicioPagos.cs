using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioPagos
{
    Task<PagosPantallaResponse> ListarAsync(CancellationToken cancelacion);

    Task<CatalogosPagoResponse> ObtenerCatalogosAsync(CancellationToken cancelacion);

    Task<PagoDetalleResponse> ObtenerAsync(int idPago, CancellationToken cancelacion);

    Task<IReadOnlyList<EncargadoPagadorResponse>> ListarEncargadosAsync(int idEstudiante, CancellationToken cancelacion);

    Task<int> RegistrarAsync(int idUsuarioAccion, RegistrarPagoRequest peticion, CancellationToken cancelacion);

    Task<int> RegistrarAbonoAsync(
        int idUsuarioAccion, int idPago, RegistrarAbonoRequest peticion, CancellationToken cancelacion);

    Task AnularAbonoAsync(int idUsuarioAccion, int idAbono, AnularPagoRequest peticion, CancellationToken cancelacion);

    Task AnularAsync(int idUsuarioAccion, int idPago, AnularPagoRequest peticion, CancellationToken cancelacion);
}