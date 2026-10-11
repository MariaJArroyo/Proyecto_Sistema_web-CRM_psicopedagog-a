
namespace MenteActiva.Models;

public class EstrategiaViewModel
{
    public int IdEstrategia { get; set; }

    public int IdPlan { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int? Orden { get; set; }

    public DateTime? FechaCreacion { get; set; }
}
