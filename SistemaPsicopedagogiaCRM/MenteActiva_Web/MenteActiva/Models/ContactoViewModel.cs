using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class ContactoViewModel
{
    [Required(ErrorMessage = "Ingrese su nombre.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su apellido.")]
    [StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    // Acepta 8888-8888 o 88888888; el guion se quita antes de mandarlo al API
    [Required(ErrorMessage = "Ingrese su teléfono.")]
    [RegularExpression(@"^\d{4}-?\d{4}$", ErrorMessage = "El teléfono debe tener 8 dígitos.")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(150)]
    public string CorreoElectronico { get; set; } = string.Empty;

    public int? IdServicioInteres { get; set; }

    [Required(ErrorMessage = "Escriba un breve mensaje.")]
    [StringLength(2000, ErrorMessage = "El mensaje no puede pasar de 2000 caracteres.")]
    public string Mensaje { get; set; } = string.Empty;

    // Campo trampa: invisible para las personas. Si viene lleno, lo lleno un bot.
    public string? SitioWeb { get; set; }
}