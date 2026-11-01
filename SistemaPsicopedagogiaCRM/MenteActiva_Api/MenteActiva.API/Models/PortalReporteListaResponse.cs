namespace MenteActiva.Api.Models;

public class PortalReporteListaResponse
{
    public int IdReporte { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public DateTime PeriodoInicio { get; set; }

    public DateTime PeriodoFin { get; set; }

    public DateTime FechaGeneracion { get; set; }
}