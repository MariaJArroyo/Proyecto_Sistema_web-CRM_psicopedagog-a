using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string Contrasena { get; set; } = string.Empty;

    // Para la bitacora de accesos. Los manda la Web, que es la que ve al
    // navegador de la persona; si no vienen, el controlador usa los de la
    // conexion. No deciden nada: solo quedan anotados. Sin StringLength a
    // proposito: un navegador con un User-Agent largo no puede dejar a nadie sin
    // entrar; el procedimiento los recorta.
    public string? IpCliente { get; set; }

    public string? Navegador { get; set; }
}
