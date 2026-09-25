namespace MenteActiva.Models;

// Lo que devuelve el API al iniciar sesion correctamente.
public class SesionViewModel
{
    public string Token { get; set; } = string.Empty;

    public DateTime Expira { get; set; }

    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public int? IdEncargado { get; set; }
}
