namespace MenteActiva.Api.Models;

// Coincide con las columnas de SP_UsuarioExterno_Listar
public class UsuarioExternoResponse
{
    public int IdEncargado { get; set; }

    public string Encargado { get; set; } = string.Empty;

    public string? Correo { get; set; }

    public string? Estudiante { get; set; }

    // Null mientras no se le haya habilitado el portal
    public int? IdUsuario { get; set; }

    // Sin correo, Sin invitar, o el estado del usuario si ya tiene
    public string Estado { get; set; } = string.Empty;

    public DateTime? UltimoAcceso { get; set; }
}
