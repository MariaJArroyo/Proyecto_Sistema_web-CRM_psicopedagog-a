using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioReporte
{
    Task<IReadOnlyList<ReporteListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde, DateOnly? hasta, string? buscar, string estado, string orden, CancellationToken cancelacion);

    Task<ReporteDetalleResponse?> ObtenerAsync(
        int idReporte, CancellationToken cancelacion);

    Task<int> GenerarAsync(
        int idUsuario, GenerarReporteRequest peticion, CancellationToken cancelacion);

    Task CambiarEstadoAsync(
        int idReporte, bool activo, CancellationToken cancelacion);
}