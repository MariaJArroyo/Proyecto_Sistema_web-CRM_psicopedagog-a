namespace MenteActiva.Api.Models;

public sealed class ReporteDetalleResponse
{
    public int IdReporte { get; set; }
    public int IdEstudiante { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFin { get; set; }
    public string? Contenido { get; set; }
    public string? RutaArchivo { get; set; }
    public bool VisibleEnPortal { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public string GeneradoPor { get; set; } = string.Empty;
}