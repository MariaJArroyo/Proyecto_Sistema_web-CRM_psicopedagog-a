namespace MenteActiva.Api.Models;

public class EncargadoEstudianteResponse
{
    public int IdEncargado { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Identificacion { get; set; }
    public string? Correo { get; set; }
    public string Parentesco { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
}