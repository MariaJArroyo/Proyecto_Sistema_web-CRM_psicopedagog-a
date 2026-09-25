namespace MenteActiva.Api.Models;

// Coincide con las columnas de SP_Usuario_Listar
public class UsuarioResponse
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Rol { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public DateTime FechaCreacion { get; set; }

    // Con valor solo en los usuarios del portal
    public int? IdEncargado { get; set; }
}
