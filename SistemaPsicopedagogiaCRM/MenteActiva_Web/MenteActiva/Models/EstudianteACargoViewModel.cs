namespace MenteActiva.Models;

public class EstudianteACargoViewModel
{
    public int IdEstudiante { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public int? Edad { get; set; }
    public int? IdNivelEducativo { get; set; }
    public string? NivelEducativo { get; set; }
    public int IdParentesco { get; set; }
    public string Parentesco { get; set; } = string.Empty;
    public bool Activo { get; set; }
}