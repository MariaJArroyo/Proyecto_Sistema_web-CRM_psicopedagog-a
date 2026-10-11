using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class AgregarAreaEstudianteRequest
{
    [Required(ErrorMessage = "El nombre del área es obligatorio.")]
    [StringLength(100)]
    public string NombreArea { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Observacion { get; set; }
}