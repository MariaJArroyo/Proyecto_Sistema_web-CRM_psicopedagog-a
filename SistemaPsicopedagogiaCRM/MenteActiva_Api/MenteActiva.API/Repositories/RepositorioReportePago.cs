using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioReportePago
{
    Task<int> ReportarAsync(
        int idUsuario, int idPago, ReportarPagoRequest peticion, decimal monto, DateTime fechaPago,
        string? archivo, string? tipoArchivo, DateTime hoy, CancellationToken cancelacion);

    Task<IReadOnlyList<PortalReportePagoResponse>> ListarPortalAsync(
        int idUsuario, int idEstudiante, CancellationToken cancelacion);

    Task<IReadOnlyList<ReportePagoResponse>> ListarAsync(string? estado, CancellationToken cancelacion);

    Task<ComprobanteReporteRegistro?> ObtenerComprobanteAsync(int idReportePago, CancellationToken cancelacion);

    Task<int> ContarPendientesAsync(CancellationToken cancelacion);

    Task<int> AprobarAsync(int idUsuarioAccion, int idReportePago, CancellationToken cancelacion);

    Task RechazarAsync(int idUsuarioAccion, int idReportePago, string motivo, CancellationToken cancelacion);
}

// Solo llama SPs. Las reglas (saldo, encargado del estudiante, estados) viven
// en la base, igual que en RepositorioPago.
public sealed class RepositorioReportePago : RepositorioBase, IRepositorioReportePago
{
    public RepositorioReportePago(IConfiguration config)
        : base(config)
    {
    }

    public Task<int> ReportarAsync(
        int idUsuario, int idPago, ReportarPagoRequest peticion, decimal monto, DateTime fechaPago,
        string? archivo, string? tipoArchivo, DateTime hoy, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", peticion.IdEstudiante);
        parametros.Add("p_IdPago", idPago);
        parametros.Add("p_IdMetodoPago", peticion.IdMetodoPago);
        parametros.Add("p_Monto", monto);
        parametros.Add("p_FechaPago", fechaPago.Date);
        parametros.Add("p_Referencia", peticion.Referencia);
        parametros.Add("p_Archivo", archivo);
        parametros.Add("p_TipoArchivo", tipoArchivo);
        parametros.Add("p_Comentario", peticion.Comentario);
        parametros.Add("p_Hoy", hoy.Date);

        return EscalarAsync("SP_Portal_ReportarPago", parametros, cancelacion);
    }

    public Task<IReadOnlyList<PortalReportePagoResponse>> ListarPortalAsync(
        int idUsuario, int idEstudiante, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);

        return ConsultarAsync<PortalReportePagoResponse>("SP_Portal_ListarReportesPago", parametros, cancelacion);
    }

    public Task<IReadOnlyList<ReportePagoResponse>> ListarAsync(string? estado, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Estado", estado);

        return ConsultarAsync<ReportePagoResponse>("SP_Pago_ListarReportes", parametros, cancelacion);
    }

    public async Task<ComprobanteReporteRegistro?> ObtenerComprobanteAsync(
        int idReportePago, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdReportePago", idReportePago);

        var filas = await ConsultarAsync<ComprobanteReporteRegistro>(
            "SP_Pago_ObtenerComprobanteReporte", parametros, cancelacion);

        return filas.SingleOrDefault();
    }

    public async Task<int> ContarPendientesAsync(CancellationToken cancelacion)
    {
        var filas = await ConsultarAsync<int>("SP_Pago_ContarReportesPendientes", null, cancelacion);

        return filas.SingleOrDefault();
    }

    public Task<int> AprobarAsync(int idUsuarioAccion, int idReportePago, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdReportePago", idReportePago);

        return EscalarAsync("SP_Pago_AprobarReporte", parametros, cancelacion);
    }

    public Task RechazarAsync(int idUsuarioAccion, int idReportePago, string motivo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdReportePago", idReportePago);
        parametros.Add("p_Motivo", motivo);

        return EjecutarAsync("SP_Pago_RechazarReporte", parametros, cancelacion);
    }
}
