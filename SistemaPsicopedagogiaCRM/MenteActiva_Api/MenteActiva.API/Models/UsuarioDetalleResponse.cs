namespace MenteActiva.Api.Models;

public class UsuarioDetalleResponse
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public int IdEstadoUsuario { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int? IdRol { get; set; }

    public string? Rol { get; set; }

    public int? IdEncargado { get; set; }

    public bool EsDelPortal => IdEncargado is not null;
}
