using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    public string Contrasena { get; set; } = string.Empty;
}
