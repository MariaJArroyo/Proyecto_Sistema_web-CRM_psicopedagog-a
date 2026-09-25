using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class CrearUsuarioRequest
{
    [Required]
    [StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int IdRol { get; set; }
}
