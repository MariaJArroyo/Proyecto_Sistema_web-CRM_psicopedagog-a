using System.Globalization;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

public interface IServicioReportesPago
{
    Task<int> ReportarAsync(int idUsuario, int idPago, ReportarPagoRequest peticion, CancellationToken cancelacion);

    Task<IReadOnlyList<PortalReportePagoResponse>> ListarPortalAsync(
        int idUsuario, int idEstudiante, CancellationToken cancelacion);

    Task<IReadOnlyList<ReportePagoResponse>> ListarAsync(string? estado, CancellationToken cancelacion);

    Task<ComprobanteDescarga?> AbrirComprobanteAsync(int idReportePago, CancellationToken cancelacion);

    Task<int> ContarPendientesAsync(CancellationToken cancelacion);

    Task<int> AprobarAsync(int idUsuarioAccion, int idReportePago, CancellationToken cancelacion);

    Task RechazarAsync(
        int idUsuarioAccion, int idReportePago, RechazarReportePagoRequest peticion, CancellationToken cancelacion);
}

public sealed record ComprobanteDescarga(Stream Contenido, string Tipo, string NombreDescarga);

// Reportes de pago de las familias. Aqui va lo que la base no puede revisar:
// el archivo (tamanio y formato), el texto del monto y la fecha de hoy en
// Costa Rica. El resto (saldo, a quien pertenece el cargo) lo validan las SPs.
public sealed class ServicioReportesPago : IServicioReportesPago
{
    private static readonly string[] Estados = { "Pendiente", "Aprobado", "Rechazado" };

    private readonly IRepositorioReportePago _repositorio;
    private readonly IAlmacenComprobantes _almacen;
    private readonly IRelojNegocio _reloj;

    public ServicioReportesPago(
        IRepositorioReportePago repositorio, IAlmacenComprobantes almacen, IRelojNegocio reloj)
    {
        _repositorio = repositorio;
        _almacen = almacen;
        _reloj = reloj;
    }

    // ---------- portal ----------

    public async Task<int> ReportarAsync(
        int idUsuario, int idPago, ReportarPagoRequest peticion, CancellationToken cancelacion)
    {
        if (idPago <= 0)
        {
            throw new ReglaNegocioException("Seleccione un cargo válido.");
        }

        var monto = LeerMonto(peticion.Monto);
        var fecha = LeerFecha(peticion.FechaPago);
        var hoy = _reloj.Hoy;

        if (fecha > hoy)
        {
            throw new ReglaNegocioException("La fecha del pago no puede ser futura.");
        }

        var referencia = Limpiar(peticion.Referencia);
        var archivo = peticion.Comprobante is { Length: > 0 } ? peticion.Comprobante : null;

        if (referencia is null && archivo is null)
        {
            throw new ReglaNegocioException("Adjunte el comprobante o escriba el número de referencia.");
        }

        if (archivo is not null && archivo.Length > AlmacenComprobantes.TamanoMaximoBytes)
        {
            throw new ReglaNegocioException(
                $"El comprobante no puede pasar de {AlmacenComprobantes.TamanoMaximoMb} MB.");
        }

        string? nombre = null;
        string? tipo = null;

        if (archivo is not null)
        {
            await using var flujo = archivo.OpenReadStream();
            (nombre, tipo) = await _almacen.GuardarAsync(flujo, cancelacion);
        }

        var limpia = new ReportarPagoRequest
        {
            IdEstudiante = peticion.IdEstudiante,
            IdMetodoPago = peticion.IdMetodoPago,
            Referencia = referencia,
            Comentario = Limpiar(peticion.Comentario)
        };

        try
        {
            return await _repositorio.ReportarAsync(
                idUsuario, idPago, limpia, monto,
                fecha.ToDateTime(TimeOnly.MinValue), nombre, tipo,
                hoy.ToDateTime(TimeOnly.MinValue), cancelacion);
        }
        catch
        {
            // Si la base no acepta el reporte, el archivo no se queda huerfano
            _almacen.Borrar(nombre);
            throw;
        }
    }

    public Task<IReadOnlyList<PortalReportePagoResponse>> ListarPortalAsync(
        int idUsuario, int idEstudiante, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException("Seleccione un estudiante válido.");
        }

        return _repositorio.ListarPortalAsync(idUsuario, idEstudiante, cancelacion);
    }

    // ---------- panel ----------

    public Task<IReadOnlyList<ReportePagoResponse>> ListarAsync(string? estado, CancellationToken cancelacion)
    {
        var normalizado = Estados.FirstOrDefault(e =>
            string.Equals(e, estado?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(estado) && normalizado is null)
        {
            throw new ReglaNegocioException("El estado indicado no es válido.");
        }

        return _repositorio.ListarAsync(normalizado, cancelacion);
    }

    public async Task<ComprobanteDescarga?> AbrirComprobanteAsync(int idReportePago, CancellationToken cancelacion)
    {
        var registro = await _repositorio.ObtenerComprobanteAsync(idReportePago, cancelacion);

        if (registro?.ArchivoComprobante is null)
        {
            return null;
        }

        var flujo = _almacen.Abrir(registro.ArchivoComprobante);

        if (flujo is null)
        {
            return null;
        }

        var tipo = string.IsNullOrWhiteSpace(registro.TipoArchivo) ? "application/octet-stream" : registro.TipoArchivo;
        var extension = Path.GetExtension(registro.ArchivoComprobante);

        return new ComprobanteDescarga(flujo, tipo, $"comprobante-{idReportePago}{extension}");
    }

    public Task<int> ContarPendientesAsync(CancellationToken cancelacion)
        => _repositorio.ContarPendientesAsync(cancelacion);

    public Task<int> AprobarAsync(int idUsuarioAccion, int idReportePago, CancellationToken cancelacion)
        => _repositorio.AprobarAsync(idUsuarioAccion, idReportePago, cancelacion);

    public Task RechazarAsync(
        int idUsuarioAccion, int idReportePago, RechazarReportePagoRequest peticion, CancellationToken cancelacion)
    {
        var motivo = peticion.Motivo?.Trim();

        if (string.IsNullOrEmpty(motivo))
        {
            throw new ReglaNegocioException("Indique el motivo: la familia lo verá en el portal.");
        }

        return _repositorio.RechazarAsync(idUsuarioAccion, idReportePago, motivo, cancelacion);
    }

    // ---------- auxiliares ----------

    // Acepta "12500", "12500.50" y "12 500,50": se quitan espacios y la coma
    // decimal se pasa a punto antes de leer con cultura invariante
    private static decimal LeerMonto(string? texto)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(" ", string.Empty).Replace(",", ".");

        if (!decimal.TryParse(limpio, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var monto)
            || monto <= 0
            || monto > 10_000_000)
        {
            throw new ReglaNegocioException("Indique un monto válido, mayor que cero.");
        }

        return Math.Round(monto, 2);
    }

    private static DateOnly LeerFecha(string? texto)
    {
        if (!DateOnly.TryParseExact(texto?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fecha))
        {
            throw new ReglaNegocioException("Indique la fecha en que hizo el pago.");
        }

        return fecha;
    }

    private static string? Limpiar(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
