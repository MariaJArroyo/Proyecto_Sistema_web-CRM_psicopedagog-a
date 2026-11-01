namespace MenteActiva.Api.Models;

public class PortalPagoDetalleResponse
{
    public PortalPagoResponse Pago { get; set; } = new();

    public List<PortalAbonoResponse> Abonos { get; set; } = [];
}