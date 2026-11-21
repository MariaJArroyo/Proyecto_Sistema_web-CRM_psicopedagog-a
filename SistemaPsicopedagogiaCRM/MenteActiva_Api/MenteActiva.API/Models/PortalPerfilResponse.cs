namespace MenteActiva.Api.Models;

public class PortalPerfilResponse
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    public List<PortalPerfilEstudianteResponse> EstudiantesACargo { get; set; } = [];
}