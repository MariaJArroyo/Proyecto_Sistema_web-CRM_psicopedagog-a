using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// =====================================================
// Pagos reportados por las familias (TB_REPORTE_PAGO)
// =====================================================

// multipart/form-data desde el portal. Monto y FechaPago llegan como texto y
// los interpreta el servicio con cultura invariante: el enlace de formularios
// usa la cultura del servidor y con es-CR "12500.50" se leeria mal.
public sealed class ReportarPagoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante válido.")]
    public int IdEstudiante { get; set; }

    // 2 = SINPE Movil, 3 = Transferencia (ids de TB_METODO_PAGO)
    [Range(2, 3, ErrorMessage = "Seleccione SINPE Móvil o transferencia.")]
    public int IdMetodoPago { get; set; }

    [Required(ErrorMessage = "Indique el monto que pagó.")]
    public string? Monto { get; set; }

    // aaaa-mm-dd
    [Required(ErrorMessage = "Indique la fecha del pago.")]
    public string? FechaPago { get; set; }

    [StringLength(100, ErrorMessage = "La referencia no puede pasar de 100 caracteres.")]
    public string? Referencia { get; set; }

    [StringLength(500, ErrorMessage = "El comentario no puede pasar de 500 caracteres.")]
    public string? Comentario { get; set; }

    // Captura o PDF del comprobante. Opcional si viene la referencia.
    public IFormFile? Comprobante { get; set; }
}

public sealed record RechazarReportePagoRequest
{
    [Required(ErrorMessage = "Indique el motivo: la familia lo verá en el portal.")]
    [StringLength(500, ErrorMessage = "El motivo no puede pasar de 500 caracteres.")]
    public string Motivo { get; init; } = string.Empty;
}

// Lo que ve la familia. Sin nombre de archivo.
public sealed class PortalReportePagoResponse
{
    public int IdReportePago { get; set; }
    public int IdPago { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public string? Referencia { get; set; }
    public bool TieneComprobante { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaRevision { get; set; }
}

// Lo que ve el panel. Tampoco lleva el nombre del archivo: el comprobante se
// pide aparte por id.
public sealed class ReportePagoResponse
{
    public int IdReportePago { get; set; }
    public int IdPago { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal MontoCargo { get; set; }
    public decimal SaldoCargo { get; set; }
    public int IdEstudiante { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public int IdEncargado { get; set; }
    public string Encargado { get; set; } = string.Empty;
    public int IdMetodoPago { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public string? Referencia { get; set; }
    public bool TieneComprobante { get; set; }
    public string? TipoArchivo { get; set; }
    public string? Comentario { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaRevision { get; set; }
    public string? RevisadoPor { get; set; }
}

public sealed class ComprobanteReporteRegistro
{
    public int IdReportePago { get; set; }
    public string? ArchivoComprobante { get; set; }
    public string? TipoArchivo { get; set; }
}
