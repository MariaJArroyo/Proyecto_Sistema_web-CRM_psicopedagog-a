
using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class ActualizarResumenEstudianteRequest
{
    [StringLength(1000, ErrorMessage = "El resumen no puede superar los 1000 caracteres.")]
    public string? Resumen { get; set; }
}
