using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public sealed record CrearCitaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante.")]
    public int IdEstudiante { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de sesión.")]
    public int IdTipoSesion { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la modalidad.")]
    public int IdModalidad { get; init; }

    // Nullable + Required: si no viene, falla la validacion en vez de llegar como 01/01/0001
    [Required(ErrorMessage = "Seleccione la fecha y la hora.")]
    public DateTime? FechaHoraInicio { get; init; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden pasar de 500 caracteres.")]
    public string? Observaciones { get; init; }
}

public sealed record ReprogramarCitaRequest
{
    [Required(ErrorMessage = "Seleccione la nueva fecha y hora.")]
    public DateTime? FechaHoraInicio { get; init; }
}

public sealed record CancelarCitaRequest
{
    [Required(ErrorMessage = "Indique el motivo de la cancelación.")]
    [StringLength(500, ErrorMessage = "El motivo no puede pasar de 500 caracteres.")]
    public string Motivo { get; init; } = string.Empty;
}
public sealed record CrearGrupoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de sesión.")]
    public int IdTipoSesion { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la modalidad.")]
    public int IdModalidad { get; init; }

    [Required(ErrorMessage = "Seleccione la fecha y la hora.")]
    public DateTime? FechaHoraInicio { get; init; }

    [StringLength(100, ErrorMessage = "El nombre del grupo no puede pasar de 100 caracteres.")]
    public string? Nombre { get; init; }

    [Range(2, 30, ErrorMessage = "El cupo del grupo debe estar entre 2 y 30.")]
    public int CupoMaximo { get; init; } = 6;

    [MinLength(1, ErrorMessage = "Agregue al menos un estudiante al grupo.")]
    public List<int> IdsEstudiantes { get; init; } = [];

    [StringLength(500, ErrorMessage = "Las observaciones no pueden pasar de 500 caracteres.")]
    public string? Observaciones { get; init; }
}

public sealed record AgregarEstudianteGrupoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante.")]
    public int IdEstudiante { get; init; }
}

public sealed record EditarGrupoRequest
{
    [StringLength(100, ErrorMessage = "El nombre del grupo no puede pasar de 100 caracteres.")]
    public string? Nombre { get; init; }

    [Range(2, 30, ErrorMessage = "El cupo del grupo debe estar entre 2 y 30.")]
    public int CupoMaximo { get; init; }
}