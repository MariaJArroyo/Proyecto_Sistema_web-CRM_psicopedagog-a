using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Solo llama SPs de pagos. El estado de cada cobro lo calcula la base
// (SP_Pago_RecalcularEstado); aqui nunca se escribe a mano.
public sealed class RepositorioPago : RepositorioBase, IRepositorioPago
{
    public RepositorioPago(IConfiguration config)
        : base(config)
    {
    }

    // ---------- consultas ----------

    public Task<IReadOnlyList<PagoListaResponse>> ListarAsync(DateTime hoy, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Hoy", hoy.Date);

        return ConsultarAsync<PagoListaResponse>("SP_Pago_Listar", parametros, cancelacion);
    }

    public Task<PagoDetalleResponse> ObtenerAsync(int idPago, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdPago", idPago);

        // Tres resultados en orden: el cobro, sus abonos y los encargados del estudiante
        return ConsultarVariosAsync("SP_Pago_Obtener", parametros, async lector =>
        {
            var pago = await lector.ReadSingleAsync<PagoEncabezadoResponse>();
            var abonos = (await lector.ReadAsync<AbonoResponse>()).AsList();
            var encargados = (await lector.ReadAsync<EncargadoPagadorResponse>()).AsList();

            return new PagoDetalleResponse(pago, abonos, encargados);
        }, cancelacion);
    }

    public Task<IReadOnlyList<EncargadoPagadorResponse>> ListarEncargadosAsync(
        int idEstudiante, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", idEstudiante);

        return ConsultarAsync<EncargadoPagadorResponse>("SP_Pago_EncargadosEstudiante", parametros, cancelacion);
    }

    public Task<IReadOnlyList<MetodoPagoResponse>> ListarMetodosAsync(CancellationToken cancelacion)
        => ConsultarAsync<MetodoPagoResponse>("SP_Pago_ListarMetodos", null, cancelacion);

    public Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion)
        => ConsultarAsync<TipoSesionResponse>("SP_ListarTiposSesion_CRM", null, cancelacion);

    // ---------- acciones ----------

    public Task<int> RegistrarAsync(
        int idUsuarioAccion, RegistrarPagoRequest peticion, DateTime hoy, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEstudiante", peticion.IdEstudiante);
        parametros.Add("p_Concepto", peticion.Concepto);
        parametros.Add("p_Monto", peticion.Monto);
        parametros.Add("p_FechaEmision", peticion.FechaEmision?.ToDateTime(TimeOnly.MinValue));
        parametros.Add("p_FechaVencimiento", peticion.FechaVencimiento?.ToDateTime(TimeOnly.MinValue));
        parametros.Add("p_Observaciones", peticion.Observaciones);
        parametros.Add("p_YaPagado", peticion.YaPagado);
        parametros.Add("p_IdEncargadoPagador", peticion.IdEncargadoPagador);
        parametros.Add("p_IdMetodoPago", peticion.IdMetodoPago);
        parametros.Add("p_Referencia", peticion.Referencia);
        parametros.Add("p_Hoy", hoy.Date);

        return EscalarAsync("SP_Pago_Registrar", parametros, cancelacion);
    }

    public Task<int> RegistrarAbonoAsync(
        int idUsuarioAccion, int idPago, RegistrarAbonoRequest peticion, DateTime fechaAbono,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdPago", idPago);
        parametros.Add("p_IdEncargadoPagador", peticion.IdEncargadoPagador);
        parametros.Add("p_IdMetodoPago", peticion.IdMetodoPago);
        parametros.Add("p_Monto", peticion.Monto);
        parametros.Add("p_FechaAbono", fechaAbono.Date);
        parametros.Add("p_Referencia", peticion.Referencia);
        parametros.Add("p_Observaciones", peticion.Observaciones);

        return EscalarAsync("SP_Pago_RegistrarAbono", parametros, cancelacion);
    }

    public Task AnularAbonoAsync(int idUsuarioAccion, int idAbono, string motivo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdAbono", idAbono);
        parametros.Add("p_Motivo", motivo);

        return EjecutarAsync("SP_Pago_AnularAbono", parametros, cancelacion);
    }

    public Task AnularAsync(int idUsuarioAccion, int idPago, string motivo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdPago", idPago);
        parametros.Add("p_Motivo", motivo);

        return EjecutarAsync("SP_Pago_Anular", parametros, cancelacion);
    }
}