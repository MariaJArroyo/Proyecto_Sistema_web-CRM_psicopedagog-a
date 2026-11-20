namespace MenteActiva.Api.Models;

public class UsuarioPerfilResponse
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int? IdRol { get; set; }
    public string? Rol { get; set; }
}
