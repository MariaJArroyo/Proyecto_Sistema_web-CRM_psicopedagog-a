using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public sealed class EstadoAreaRequest
{
    [Required(ErrorMessage = "Indique el estado del área.")]
    public bool? Activo { get; set; }
}