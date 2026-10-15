using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public sealed class RepositorioReporte : RepositorioBase, IRepositorioReporte
{
    public RepositorioReporte(IConfiguration config)
        : base(config)
    {
    }

    public Task<IReadOnlyList<ReporteListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde, DateOnly? hasta, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Desde", desde?.ToDateTime(TimeOnly.MinValue).Date);
        parametros.Add("p_Hasta", hasta?.ToDateTime(TimeOnly.MinValue).Date);

        return ConsultarAsync<ReporteListaResponse>("SP_Reporte_Listar", parametros, cancelacion);
    }

    public async Task<ReporteDetalleResponse?> ObtenerAsync(int idReporte, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdReporte", idReporte);

        var filas = await ConsultarAsync<ReporteDetalleResponse>(
            "SP_Reporte_ObtenerDetalle", parametros, cancelacion);

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

        parametros.Add("p_Desde", peticion.PeriodoInicio!.Value.ToDateTime(TimeOnly.MinValue));

        parametros.Add("p_Hasta", peticion.PeriodoFin!.Value.ToDateTime(TimeOnly.MinValue));

        return EscalarAsync("SP_Reporte_Generar", parametros, cancelacion);
    }
}