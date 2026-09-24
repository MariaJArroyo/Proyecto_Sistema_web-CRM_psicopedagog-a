namespace MenteActiva.Models;

public class FichaEstudianteViewModel
{
    public int IdEstudiante { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string PrimerApellido { get; set; } = string.Empty;

    public string? SegundoApellido { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public string? NivelEducativo { get; set; }

    public string? Institucion { get; set; }

    public string? AreasDificultad { get; set; }

    public string? Observaciones { get; set; }

    public string? NecesidadesApoyo { get; set; }
}