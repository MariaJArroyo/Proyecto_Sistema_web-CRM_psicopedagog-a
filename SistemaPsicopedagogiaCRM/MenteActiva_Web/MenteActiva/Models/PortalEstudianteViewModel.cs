namespace MenteActiva.Models;

public class PortalEstudianteViewModel
{
    public int IdEstudiante { get; set; }

    public string Estudiante { get; set; } = string.Empty;

    public bool EsPrincipal { get; set; }
}