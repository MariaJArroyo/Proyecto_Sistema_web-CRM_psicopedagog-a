using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public sealed class RepositorioReporte
    : RepositorioBase, IRepositorioReporte
{
    public RepositorioReporte(IConfiguration config)
        : base(config)
    {
    }

    public Task<IReadOnlyList<ReporteListaResponse>> ListarAsync(
        int? idEstudiante,DateOnly? desde, DateOnly? hasta,string? buscar, string estado, string orden,CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdEstudiante", idEstudiante);

        parametros.Add("p_Desde",desde?.ToDateTime(TimeOnly.MinValue).Date);

        parametros.Add("p_Hasta", hasta?.ToDateTime(TimeOnly.MinValue).Date);

        parametros.Add("p_Buscar", buscar?.Trim());
        parametros.Add("p_Estado", estado);
        parametros.Add("p_Orden", orden);

        return ConsultarAsync<ReporteListaResponse>("SP_Reporte_Listar", parametros,cancelacion);
    }

    public async Task<ReporteDetalleResponse?> ObtenerAsync(
        int idReporte, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters(); parametros.Add("p_IdReporte", idReporte);

        var filas = await ConsultarAsync<ReporteDetalleResponse>("SP_Reporte_ObtenerDetalle",parametros,cancelacion);

        return filas.Count > 0 ? filas[0] : null;
    }

    public Task<int> GenerarAsync(
        int idUsuario,
        GenerarReporteRequest peticion,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", peticion.IdEstudiante);
        parametros.Add("p_Titulo", peticion.Titulo.Trim());

        parametros.Add("p_Desde",peticion.PeriodoInicio!.Value.ToDateTime(TimeOnly.MinValue));

        parametros.Add("p_Hasta",peticion.PeriodoFin!.Value.ToDateTime(TimeOnly.MinValue));

        parametros.Add("p_IncluirDatosGenerales",peticion.IncluirDatosGenerales);

        parametros.Add("p_IncluirSesiones",peticion.IncluirSesiones);

        parametros.Add("p_IncluirPlanes", peticion.IncluirPlanes);

        return EscalarAsync("SP_Reporte_Generar", parametros,   cancelacion);
    }

    public Task CambiarEstadoAsync(
        int idReporte, bool activo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdReporte", idReporte); parametros.Add("p_Activo", activo);

        return EjecutarAsync(
            "SP_Reporte_CambiarEstado", parametros,cancelacion);
    }
}