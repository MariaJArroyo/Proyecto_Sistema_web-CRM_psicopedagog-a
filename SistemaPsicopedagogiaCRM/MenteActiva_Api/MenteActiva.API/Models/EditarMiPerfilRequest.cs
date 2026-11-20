namespace MenteActiva.Api.Models;

public class EditarMiPerfilRequest
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
}
