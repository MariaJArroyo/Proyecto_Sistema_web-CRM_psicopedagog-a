using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// Lo que llega del formulario de registro. Los limites de texto son los mismos
// que las columnas de TB_SESION: si no coinciden, el error sale de MySQL como
// 500 en vez de como mensaje para la persona.
public sealed record RegistrarSesionRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante.")]
    public int IdEstudiante { get; init; }

    // Opcional: la sesion puede nacer de una cita o registrarse suelta
    public int? IdCita { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de sesión.")]
    public int IdTipoSesion { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de atención.")]
    public int IdTipoAtencion { get; init; }

    // Nullable + Required: si no viene, falla la validacion en vez de llegar
    // como 01/01/0001
    [Required(ErrorMessage = "Indique la fecha y la hora de la sesión.")]
    public DateTime? FechaHoraInicio { get; init; }

    [Range(15, 240, ErrorMessage = "La duración debe estar entre 15 y 240 minutos.")]
    public int DuracionMinutos { get; init; }

    [Required(ErrorMessage = "Indique el objetivo de la sesión.")]
    [StringLength(500, ErrorMessage = "El objetivo no puede pasar de 500 caracteres.")]
    public string Objetivo { get; init; } = string.Empty;

    [Required(ErrorMessage = "Indique el tema trabajado.")]
    [StringLength(500, ErrorMessage = "El tema trabajado no puede pasar de 500 caracteres.")]
    public string TemaTrabajado { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Las actividades realizadas no pueden pasar de 1000 caracteres.")]
    public string? ActividadesRealizadas { get; init; }

    [StringLength(1000, ErrorMessage = "Los acuerdos no pueden pasar de 1000 caracteres.")]
    public string? Acuerdos { get; init; }

    [StringLength(1000, ErrorMessage = "Los objetivos alcanzados no pueden pasar de 1000 caracteres.")]
    public string? ObjetivosAlcanzados { get; init; }

    [StringLength(1000, ErrorMessage = "Las habilidades desarrolladas no pueden pasar de 1000 caracteres.")]
    public string? HabilidadesDesarrolladas { get; init; }

    [StringLength(1000, ErrorMessage = "Los avances no pueden pasar de 1000 caracteres.")]
    public string? Avances { get; init; }

    [StringLength(1000, ErrorMessage = "Las recomendaciones no pueden pasar de 1000 caracteres.")]
    public string? Recomendaciones { get; init; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden pasar de 1000 caracteres.")]
    public string? Observaciones { get; init; }
}

// La edicion no mueve el estudiante ni la cita de origen: eso seria otra sesion.
public sealed record EditarSesionRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de sesión.")]
    public int IdTipoSesion { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de atención.")]
    public int IdTipoAtencion { get; init; }

    [Required(ErrorMessage = "Indique la fecha y la hora de la sesión.")]
    public DateTime? FechaHoraInicio { get; init; }

    [Range(15, 240, ErrorMessage = "La duración debe estar entre 15 y 240 minutos.")]
    public int DuracionMinutos { get; init; }

    [Required(ErrorMessage = "Indique el objetivo de la sesión.")]
    [StringLength(500, ErrorMessage = "El objetivo no puede pasar de 500 caracteres.")]
    public string Objetivo { get; init; } = string.Empty;

    [Required(ErrorMessage = "Indique el tema trabajado.")]
    [StringLength(500, ErrorMessage = "El tema trabajado no puede pasar de 500 caracteres.")]
    public string TemaTrabajado { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Las actividades realizadas no pueden pasar de 1000 caracteres.")]
    public string? ActividadesRealizadas { get; init; }

    [StringLength(1000, ErrorMessage = "Los acuerdos no pueden pasar de 1000 caracteres.")]
    public string? Acuerdos { get; init; }

    [StringLength(1000, ErrorMessage = "Los objetivos alcanzados no pueden pasar de 1000 caracteres.")]
    public string? ObjetivosAlcanzados { get; init; }

    [StringLength(1000, ErrorMessage = "Las habilidades desarrolladas no pueden pasar de 1000 caracteres.")]
    public string? HabilidadesDesarrolladas { get; init; }

    [StringLength(1000, ErrorMessage = "Los avances no pueden pasar de 1000 caracteres.")]
    public string? Avances { get; init; }

    [StringLength(1000, ErrorMessage = "Las recomendaciones no pueden pasar de 1000 caracteres.")]
    public string? Recomendaciones { get; init; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden pasar de 1000 caracteres.")]
    public string? Observaciones { get; init; }
}
