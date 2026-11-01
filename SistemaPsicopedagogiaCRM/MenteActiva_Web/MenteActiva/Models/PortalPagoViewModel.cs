namespace MenteActiva.Models;

public class PortalPagoViewModel
{
    public int IdPago { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public decimal Abonado { get; set; }

    public decimal Saldo { get; set; }

    public DateTime FechaEmision { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int IdEstadoPago { get; set; }

    public string Estado { get; set; } = string.Empty;
}