namespace MenteActiva.Models;

public class UsuarioExternoViewModel
{
    public int IdEncargado { get; set; }

    public string Encargado { get; set; } = string.Empty;

    public string? Correo { get; set; }

    public string? Estudiante { get; set; }

    public int? IdUsuario { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? UltimoAcceso { get; set; }

    // Solo se puede invitar a quien tenga correo y todavia no tenga cuenta
    public bool SePuedeInvitar => IdUsuario is null && !string.IsNullOrWhiteSpace(Correo);
}
