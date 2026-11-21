namespace MenteActiva.Api.Models;

public class PortalPerfilEstudianteResponse
{
    public int IdEstudiante { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public string? NivelEducativo { get; set; }
    public string? Institucion { get; set; }
    public string Parentesco { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public bool EsPrincipal { get; set; }

    public List<PortalPerfilEstudianteResponse> Estudiantes { get; set; } = [];
}