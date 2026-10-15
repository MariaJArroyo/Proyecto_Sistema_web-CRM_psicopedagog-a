using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioReportes
{
    Task<IReadOnlyList<ReporteListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde, DateOnly? hasta, CancellationToken cancelacion);

    Task<ReporteDetalleResponse> ObtenerAsync(int idReporte, CancellationToken cancelacion);

    Task<ReporteDetalleResponse> GenerarAsync(int idUsuario, GenerarReporteRequest peticion, CancellationToken cancelacion);

}