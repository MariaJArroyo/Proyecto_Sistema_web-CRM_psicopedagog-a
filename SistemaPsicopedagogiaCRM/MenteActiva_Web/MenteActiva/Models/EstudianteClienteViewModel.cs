using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class EstudianteClienteViewModel
{
    [Required(ErrorMessage = "El nombre del estudiante es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del estudiante es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar el parentesco.")]
    public int IdParentesco { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }

    public int? IdNivelEducativo { get; set; }
}