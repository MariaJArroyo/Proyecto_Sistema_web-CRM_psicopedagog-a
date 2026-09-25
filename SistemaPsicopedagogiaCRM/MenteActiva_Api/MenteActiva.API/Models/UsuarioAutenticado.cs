namespace MenteActiva.Api.Models;

// Lo que devuelve SP_Usuario_ObtenerPorCorreo. No sale del API: lleva el hash
// y solo lo usa el servicio de autenticacion para compararlo.
public class UsuarioAutenticado
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string ContrasenaHash { get; set; } = string.Empty;

    public int? IdEncargado { get; set; }

    public int IdEstadoUsuario { get; set; }

    public string EstadoUsuario { get; set; } = string.Empty;

    public int IntentosFallidos { get; set; }

    public int? IdRol { get; set; }

    public string? Rol { get; set; }
}
