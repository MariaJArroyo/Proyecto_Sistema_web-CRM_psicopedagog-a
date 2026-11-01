namespace MenteActiva.Models;

public class PortalReporteListaViewModel
{
    public int IdReporte { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public DateTime PeriodoInicio { get; set; }

    public DateTime PeriodoFin { get; set; }

    public DateTime FechaGeneracion { get; set; }
}