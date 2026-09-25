using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class RestablecerRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string ContrasenaNueva { get; set; } = string.Empty;
}
