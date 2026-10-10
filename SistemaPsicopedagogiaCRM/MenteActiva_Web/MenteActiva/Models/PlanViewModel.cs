namespace MenteActiva.Models;

public class PlanViewModel
{
    public int IdPlan { get; set; }

    public int IdEstudiante { get; set; }

    public string Estudiante { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string ObjetivoGeneral { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int Estrategias { get; set; }

    public int Actividades { get; set; }

    public int ActividadesCompletadas { get; set; }

    public string? Observaciones { get; set; }
}