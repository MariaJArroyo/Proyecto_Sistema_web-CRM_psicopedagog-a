using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class EstudianteClienteRequest
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Apellido { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int IdParentesco { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    public int? IdNivelEducativo { get; set; }
}