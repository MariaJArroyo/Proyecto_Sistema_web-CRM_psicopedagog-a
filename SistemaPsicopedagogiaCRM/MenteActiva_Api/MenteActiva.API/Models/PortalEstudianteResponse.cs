namespace MenteActiva.Api.Models;

public class PortalEstudianteResponse
{
    public int IdEstudiante { get; set; }

    public string Estudiante { get; set; } = string.Empty;

    public bool EsPrincipal { get; set; }
}