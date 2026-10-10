namespace MenteActiva.Api.Models;

public class PlanCrearRequest
{
    public int IdEstudiante { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string ObjetivoGeneral { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public int IdEstadoPlan { get; set; }
}