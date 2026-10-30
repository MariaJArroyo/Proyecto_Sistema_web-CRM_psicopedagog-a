using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Reglas de pagos que dependen de "hoy" (hora de Costa Rica, no la del
// servidor). Saldos, encargados asociados y estados los validan las SPs.
public sealed class ServicioPagos : IServicioPagos
{
    private readonly IRepositorioPago _repositorio;
    private readonly IRelojNegocio _reloj;

    public ServicioPagos(IRepositorioPago repositorio, IRelojNegocio reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    public async Task<PagosPantallaResponse> ListarAsync(CancellationToken cancelacion)
    {
        var pagos = await _repositorio.ListarAsync(Hoy(), cancelacion);
        var tipos = await _repositorio.ListarTiposSesionAsync(cancelacion);

        var sinPrecio = tipos
            .Where(t => t.Precio is null or <= 0)
            .Select(t => t.Nombre)
            .ToList();

        return new PagosPantallaResponse(pagos, sinPrecio);
    }

    public async Task<CatalogosPagoResponse> ObtenerCatalogosAsync(CancellationToken cancelacion)
        => new(await _repositorio.ListarMetodosAsync(cancelacion),
               await _repositorio.ListarTiposSesionAsync(cancelacion));

    public Task<PagoDetalleResponse> ObtenerAsync(int idPago, CancellationToken cancelacion)
        => _repositorio.ObtenerAsync(idPago, cancelacion);

    public Task<IReadOnlyList<EncargadoPagadorResponse>> ListarEncargadosAsync(
        int idEstudiante, CancellationToken cancelacion)
        => _repositorio.ListarEncargadosAsync(idEstudiante, cancelacion);

    // =====================================================
    // ACCIONES
    // =====================================================

    public Task<int> RegistrarAsync(int idUsuarioAccion, RegistrarPagoRequest peticion, CancellationToken cancelacion)
    {
        var hoy = _reloj.Hoy;

        if (peticion.FechaEmision > hoy)
        {
            throw new ReglaNegocioException("La fecha del cobro no puede ser futura.");
        }

        if (peticion.FechaVencimiento < (peticion.FechaEmision ?? hoy))
        {
            throw new ReglaNegocioException("El vencimiento no puede ser anterior a la fecha del cobro.");
        }

        if (peticion.YaPagado && (peticion.IdEncargadoPagador is null || peticion.IdMetodoPago is null))
        {
            throw new ReglaNegocioException("Si el pago ya se recibió, indique quién pagó y el método de pago.");
        }

        return _repositorio.RegistrarAsync(
            idUsuarioAccion, Normalizar(peticion), hoy.ToDateTime(TimeOnly.MinValue), cancelacion);
    }

    public Task<int> RegistrarAbonoAsync(
        int idUsuarioAccion, int idPago, RegistrarAbonoRequest peticion, CancellationToken cancelacion)
    {
        var hoy = _reloj.Hoy;
        var fecha = peticion.FechaAbono ?? hoy;

        if (fecha > hoy)
        {
            throw new ReglaNegocioException("La fecha del pago no puede ser futura.");
        }

        var limpia = peticion with
        {
            Referencia = Limpiar(peticion.Referencia),
            Observaciones = Limpiar(peticion.Observaciones)
        };

        return _repositorio.RegistrarAbonoAsync(
            idUsuarioAccion, idPago, limpia, fecha.ToDateTime(TimeOnly.MinValue), cancelacion);
    }

    public Task AnularAbonoAsync(
        int idUsuarioAccion, int idAbono, AnularPagoRequest peticion, CancellationToken cancelacion)
        => _repositorio.AnularAbonoAsync(idUsuarioAccion, idAbono, peticion.Motivo.Trim(), cancelacion);

    public Task AnularAsync(
        int idUsuarioAccion, int idPago, AnularPagoRequest peticion, CancellationToken cancelacion)
        => _repositorio.AnularAsync(idUsuarioAccion, idPago, peticion.Motivo.Trim(), cancelacion);

    // =====================================================
    // AUXILIARES
    // =====================================================

    private DateTime Hoy() => _reloj.Hoy.ToDateTime(TimeOnly.MinValue);

    // Si no se cobro todavia, quien pago y el metodo no aplican
    private static RegistrarPagoRequest Normalizar(RegistrarPagoRequest peticion) => peticion with
    {
        Concepto = peticion.Concepto.Trim(),
        Observaciones = Limpiar(peticion.Observaciones),
        Referencia = peticion.YaPagado ? Limpiar(peticion.Referencia) : null,
        IdEncargadoPagador = peticion.YaPagado ? peticion.IdEncargadoPagador : null,
        IdMetodoPago = peticion.YaPagado ? peticion.IdMetodoPago : null
    };

    private static string? Limpiar(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}