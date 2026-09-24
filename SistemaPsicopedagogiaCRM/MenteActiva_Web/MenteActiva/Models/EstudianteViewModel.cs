namespace MenteActiva.Models;

public class EstudianteViewModel
{
    public int IdEstudiante { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public string? NivelEducativo { get; set; }
    public string? Institucion { get; set; }
    public string? Encargado { get; set; }
}