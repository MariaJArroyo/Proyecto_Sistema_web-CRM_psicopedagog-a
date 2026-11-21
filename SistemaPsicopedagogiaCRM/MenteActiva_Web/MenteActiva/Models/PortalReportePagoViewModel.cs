namespace MenteActiva.Models;

// Un pago que la familia reporto desde el portal y su revision
public class PortalReportePagoViewModel
{
    public int IdReportePago { get; set; }

    public int IdPago { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public string MetodoPago { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public DateTime FechaPago { get; set; }

    public string? Referencia { get; set; }

    public bool TieneComprobante { get; set; }

    // Pendiente, Aprobado o Rechazado
    public string Estado { get; set; } = string.Empty;

    public string? MotivoRechazo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaRevision { get; set; }
}
