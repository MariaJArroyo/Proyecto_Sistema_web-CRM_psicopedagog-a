using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class AgregarNecesidadEstudianteRequest
{
    [Required(ErrorMessage = "La necesidad de apoyo es obligatoria.")]
    [StringLength(100)]
    public string NombreNecesidad { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Observacion { get; set; }
}