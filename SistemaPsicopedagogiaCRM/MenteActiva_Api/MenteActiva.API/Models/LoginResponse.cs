namespace MenteActiva.Api.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime Expira { get; set; }

    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    // Solo viene con valor en los usuarios del portal
    public int? IdEncargado { get; set; }
}
