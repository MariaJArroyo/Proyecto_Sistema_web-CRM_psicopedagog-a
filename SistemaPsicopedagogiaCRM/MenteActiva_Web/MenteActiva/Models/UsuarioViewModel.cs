namespace MenteActiva.Models;

public class UsuarioViewModel
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Rol { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int? IdEncargado { get; set; }
}
