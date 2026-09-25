using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "Ingrese el nombre completo.")]
    [StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un rol.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol.")]
    public int IdRol { get; set; }
}
