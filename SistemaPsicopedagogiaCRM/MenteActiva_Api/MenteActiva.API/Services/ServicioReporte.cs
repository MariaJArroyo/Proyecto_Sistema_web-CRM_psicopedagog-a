using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;


public sealed class ServicioReportes : IServicioReportes
{
    private const string ReporteNoDisponible = "El reporte solicitado no está disponible.";

    private readonly IRepositorioReporte _repositorio;

    public ServicioReportes(IRepositorioReporte repositorio)
    {
        _repositorio = repositorio;
    }

    
    public Task<IReadOnlyList<ReporteListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde, DateOnly? hasta, CancellationToken cancelacion)
        => _repositorio.ListarAsync(idEstudiante, desde, hasta, cancelacion);

    public async Task<ReporteDetalleResponse> ObtenerAsync(int idReporte, CancellationToken cancelacion)
    {
        var reporte = await _repositorio.ObtenerAsync(idReporte, cancelacion);

        return reporte ?? throw new ReglaNegocioException(ReporteNoDisponible);
    }

    public async Task<ReporteDetalleResponse> GenerarAsync(
    int idUsuario,
    GenerarReporteRequest peticion, CancellationToken cancelacion)
    {
        if (peticion.IdEstudiante <= 0
            || string.IsNullOrWhiteSpace(peticion.Titulo)
            || peticion.Titulo.Length > 150
            || peticion.PeriodoInicio is null
            || peticion.PeriodoFin is null
            || peticion.PeriodoFin < peticion.PeriodoInicio)
        {
            throw new ReglaNegocioException(
                "Seleccione estudiante, título y un período válido.");
        }

        var idReporte = await _repositorio.GenerarAsync(idUsuario, peticion, cancelacion);

        return await ObtenerAsync(idReporte, cancelacion);
    }
}