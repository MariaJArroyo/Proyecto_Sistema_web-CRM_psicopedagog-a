namespace MenteActiva.API.Models;

public class EstrategiaCrearRequest
{
    public int IdPlan { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int? Orden { get; set; }
}