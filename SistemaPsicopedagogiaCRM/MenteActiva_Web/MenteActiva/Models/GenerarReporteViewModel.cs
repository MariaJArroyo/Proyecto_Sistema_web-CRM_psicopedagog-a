using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public sealed class GenerarReporteViewModel
{
    [Range(1, int.MaxValue)]
    public int IdEstudiante { get; set; }

    [Required, StringLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    public DateOnly? PeriodoInicio { get; set; }

    [Required]
    public DateOnly? PeriodoFin { get; set; }
}