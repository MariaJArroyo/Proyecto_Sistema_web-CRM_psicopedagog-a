namespace MenteActiva.Models;

public class PortalPerfilViewModel
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    public List<PortalPerfilEstudiantesViewModel> EstudiantesACargo { get; set; } = [];

    // Solo para el selector del navbar
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];
    public int IdEstudiante { get; set; }
}
