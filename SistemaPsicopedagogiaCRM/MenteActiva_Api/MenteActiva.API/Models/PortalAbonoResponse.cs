namespace MenteActiva.Api.Models;

public class PortalAbonoResponse
{
    public int IdAbono { get; set; }

    public int IdPago { get; set; }

    public DateTime FechaAbono { get; set; }

    public decimal Monto { get; set; }

    public string MetodoPago { get; set; } = string.Empty;

    public string? Referencia { get; set; }
}