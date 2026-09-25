using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class RecuperarContrasenaViewModel
{
    [Required(ErrorMessage = "Ingrese su correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string CorreoElectronico { get; set; } = string.Empty;
}
