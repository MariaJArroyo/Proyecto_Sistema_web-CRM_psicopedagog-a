using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

public sealed class ServicioReportes : IServicioReportes
{
    private const string ReporteNoDisponible =
        "El reporte solicitado no está disponible.";

    private readonly IRepositorioReporte _repositorio;

    public ServicioReportes(IRepositorioReporte repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<IReadOnlyList<ReporteListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde,  DateOnly? hasta,string? buscar,string estado, string orden,CancellationToken cancelacion)
    {
        if (!new[] { "activos", "inactivos", "todos" }.Contains(estado)
            || !new[] { "az", "za" }.Contains(orden)
            || (desde.HasValue && hasta.HasValue && hasta < desde)
            || buscar?.Length > 150)
        {
            throw new ReglaNegocioException(  "Revise el estado, orden, nombre y fechas del filtro.");
        }

        return _repositorio.ListarAsync( idEstudiante, desde, hasta, buscar, estado, orden,  cancelacion);
    }

    public async Task<ReporteDetalleResponse> ObtenerAsync(int idReporte,CancellationToken cancelacion)
    {
        var reporte = await _repositorio.ObtenerAsync(idReporte,cancelacion);

        return reporte
            ?? throw new ReglaNegocioException(ReporteNoDisponible);
    }

    public async Task<ReporteDetalleResponse?> GenerarAsync(int idUsuario,GenerarReporteRequest peticion,CancellationToken cancelacion)
    {
        peticion.IncluirDatosGenerales = true;
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

        if (!peticion.IncluirDatosGenerales && !peticion.IncluirSesiones && !peticion.IncluirPlanes)
        {
            throw new ReglaNegocioException(
                "Seleccione al menos una sección del reporte.");
        }

        var idReporte = await _repositorio.GenerarAsync(idUsuario,peticion,cancelacion);

        if (idReporte == 0)
        {
            return null;
        }

        return await ObtenerAsync(idReporte, cancelacion);
    }

    public Task CambiarEstadoAsync( int idReporte,bool activo,CancellationToken cancelacion)
    {
        if (idReporte <= 0)
        {
            throw new ReglaNegocioException( "El reporte indicado no es válido.");
        }

        return _repositorio.CambiarEstadoAsync(idReporte,activo,cancelacion);
    }
}