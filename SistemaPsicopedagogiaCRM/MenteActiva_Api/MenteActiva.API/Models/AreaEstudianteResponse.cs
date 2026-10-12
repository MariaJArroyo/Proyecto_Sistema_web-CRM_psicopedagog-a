namespace MenteActiva.Api.Models;

public class AreaEstudianteResponse
{
    public int IdEstudianteArea { get; set; }
    public int IdAreaDificultad { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Observacion { get; set; }
}
