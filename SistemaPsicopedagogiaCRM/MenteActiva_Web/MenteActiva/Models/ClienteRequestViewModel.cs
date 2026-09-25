using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class ClienteRequestViewModel
{
    [Required(ErrorMessage = "El nombre del encargado es obligatorio.")]
    [Display(Name = "Nombre del encargado")]
    public string NombreEncargado { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del encargado es obligatorio.")]
    [Display(Name = "Apellido del encargado")]
    public string ApellidoEncargado { get; set; } = string.Empty;

    [Required(ErrorMessage = "El telefono es obligatorio.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El telefono debe tener 8 digitos, sin espacios ni guiones.")]
    [Display(Name = "Telefono")]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo no es valido.")]
    [Display(Name = "Correo")]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un servicio de interes.")]
    [Display(Name = "Servicio de interes")]
    public int IdServicioInteres { get; set; }

    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
}