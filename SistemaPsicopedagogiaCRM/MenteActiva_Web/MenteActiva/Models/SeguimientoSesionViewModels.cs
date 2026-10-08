using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

// Ojo con el nombre: SesionViewModel ya existe y es la sesion de USUARIO que
// devuelve el login. Esto es la sesion de ATENCION, que es otra cosa.

public sealed class CatalogosSesionViewModel
{
    public List<TipoSesionViewModel> TiposSesion { get; set; } = new();
    public List<TipoAtencionViewModel> TiposAtencion { get; set; } = new();
}

public sealed class TipoAtencionViewModel
{
    public int IdTipoAtencion { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class RegistrarSesionViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante.")]
    public int IdEstudiante { get; set; }

    // El select de cita de origen manda 0 cuando se deja vacio
    public int? IdCita { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de sesión.")]
    public int IdTipoSesion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de atención.")]
    public int IdTipoAtencion { get; set; }

    [Required(ErrorMessage = "Indique la fecha y la hora de la sesión.")]
    public DateTime? FechaHoraInicio { get; set; }

    [Range(15, 240, ErrorMessage = "La duración debe estar entre 15 y 240 minutos.")]
    public int DuracionMinutos { get; set; }

    [Required(ErrorMessage = "Indique el objetivo de la sesión.")]
    [StringLength(500, ErrorMessage = "El objetivo no puede pasar de 500 caracteres.")]
    public string Objetivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el tema trabajado.")]
    [StringLength(500, ErrorMessage = "El tema trabajado no puede pasar de 500 caracteres.")]
    public string TemaTrabajado { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Las actividades realizadas no pueden pasar de 1000 caracteres.")]
    public string? ActividadesRealizadas { get; set; }

    [StringLength(1000, ErrorMessage = "Los acuerdos no pueden pasar de 1000 caracteres.")]
    public string? Acuerdos { get; set; }

    [StringLength(1000, ErrorMessage = "Los objetivos alcanzados no pueden pasar de 1000 caracteres.")]
    public string? ObjetivosAlcanzados { get; set; }

    [StringLength(1000, ErrorMessage = "Las habilidades desarrolladas no pueden pasar de 1000 caracteres.")]
    public string? HabilidadesDesarrolladas { get; set; }

    [StringLength(1000, ErrorMessage = "Los avances no pueden pasar de 1000 caracteres.")]
    public string? Avances { get; set; }

    [StringLength(1000, ErrorMessage = "Las recomendaciones no pueden pasar de 1000 caracteres.")]
    public string? Recomendaciones { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden pasar de 1000 caracteres.")]
    public string? Observaciones { get; set; }
}

// La edicion no mueve el estudiante ni la cita de origen
public sealed class EditarSesionViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de sesión.")]
    public int IdTipoSesion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de atención.")]
    public int IdTipoAtencion { get; set; }

    [Required(ErrorMessage = "Indique la fecha y la hora de la sesión.")]
    public DateTime? FechaHoraInicio { get; set; }

    [Range(15, 240, ErrorMessage = "La duración debe estar entre 15 y 240 minutos.")]
    public int DuracionMinutos { get; set; }

    [Required(ErrorMessage = "Indique el objetivo de la sesión.")]
    [StringLength(500, ErrorMessage = "El objetivo no puede pasar de 500 caracteres.")]
    public string Objetivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el tema trabajado.")]
    [StringLength(500, ErrorMessage = "El tema trabajado no puede pasar de 500 caracteres.")]
    public string TemaTrabajado { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Las actividades realizadas no pueden pasar de 1000 caracteres.")]
    public string? ActividadesRealizadas { get; set; }

    [StringLength(1000, ErrorMessage = "Los acuerdos no pueden pasar de 1000 caracteres.")]
    public string? Acuerdos { get; set; }

    [StringLength(1000, ErrorMessage = "Los objetivos alcanzados no pueden pasar de 1000 caracteres.")]
    public string? ObjetivosAlcanzados { get; set; }

    [StringLength(1000, ErrorMessage = "Las habilidades desarrolladas no pueden pasar de 1000 caracteres.")]
    public string? HabilidadesDesarrolladas { get; set; }

    [StringLength(1000, ErrorMessage = "Los avances no pueden pasar de 1000 caracteres.")]
    public string? Avances { get; set; }

    [StringLength(1000, ErrorMessage = "Las recomendaciones no pueden pasar de 1000 caracteres.")]
    public string? Recomendaciones { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden pasar de 1000 caracteres.")]
    public string? Observaciones { get; set; }
}
