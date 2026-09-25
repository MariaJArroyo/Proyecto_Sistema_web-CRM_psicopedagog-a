using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string Contrasena { get; set; } = string.Empty;
}
