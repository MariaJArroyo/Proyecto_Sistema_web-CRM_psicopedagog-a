using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class DefinirContrasenaViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña nueva.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Contrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repita la contraseña.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las dos contraseñas no coinciden.")]
    public string ConfirmarContrasena { get; set; } = string.Empty;
}
