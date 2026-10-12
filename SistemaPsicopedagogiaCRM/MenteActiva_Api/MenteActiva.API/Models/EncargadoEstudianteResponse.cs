namespace MenteActiva.Api.Models;

public class EncargadoEstudianteResponse
{
    public int IdEstudianteEncargado { get; set; }
    public int IdEncargado { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public int IdParentesco { get; set; }
    public string Parentesco { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
}