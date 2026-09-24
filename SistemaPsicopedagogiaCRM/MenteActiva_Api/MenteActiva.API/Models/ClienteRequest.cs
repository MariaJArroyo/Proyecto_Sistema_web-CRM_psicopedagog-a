using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class ClienteRequest
{
    [Required]
    public string NombreEncargado { get; set; } = string.Empty;

    [Required]
    public string ApellidoEncargado { get; set; } = string.Empty;

    [Required]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress]
    public string? Correo { get; set; }

    [Required]
    public int IdServicioInteres { get; set; }

    [Required]
    public string NombreEstudiante { get; set; } = string.Empty;

    [Required]
    public string ApellidoEstudiante { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}