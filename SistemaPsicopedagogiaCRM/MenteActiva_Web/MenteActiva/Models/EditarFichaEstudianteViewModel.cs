using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class EditarFichaEstudianteViewModel
{
    public int IdEstudiante { get; set; }

    [Required(ErrorMessage = "El nombre del estudiante es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El primer apellido es obligatorio.")]
    [StringLength(100)]
    public string PrimerApellido { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SegundoApellido { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }

    public int? IdNivelEducativo { get; set; }

    [StringLength(100)]
    public string? Institucion { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [StringLength(1000)]
    public string? NecesidadesApoyo { get; set; }
}