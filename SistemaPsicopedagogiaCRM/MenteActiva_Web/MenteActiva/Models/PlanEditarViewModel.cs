
namespace MenteActiva.Models;

public class PlanEditarViewModel
{
    public string Titulo { get; set; } = string.Empty;

    public string ObjetivoGeneral { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }
}
