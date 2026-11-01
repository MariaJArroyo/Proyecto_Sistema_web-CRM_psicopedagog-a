namespace MenteActiva.Api.Models;

public class PortalProgresoResponse
{
    public List<PortalProgresoSesionResponse> Sesiones { get; set; } = [];

    public List<PortalReporteListaResponse> Reportes { get; set; } = [];
}