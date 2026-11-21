using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class GuardarInfoAcademicaRequest
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(100)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El detalle es obligatorio.")]
    [StringLength(1000)]
    public string Detalle { get; set; } = string.Empty;
}

public sealed class InfoAcademicaResponse
{
    public int IdInfoAcademica { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Detalle { get; set; } = string.Empty;
}