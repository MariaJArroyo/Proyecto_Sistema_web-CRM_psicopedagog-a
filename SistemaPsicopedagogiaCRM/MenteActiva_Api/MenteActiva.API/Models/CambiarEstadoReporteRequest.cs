using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public sealed class CambiarEstadoReporteRequest
{
    [Required]
    public bool? Activo { get; set; }
}