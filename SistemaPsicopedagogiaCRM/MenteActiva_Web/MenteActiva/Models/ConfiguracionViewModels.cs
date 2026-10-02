using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public sealed class FranjaHorarioViewModel
{
    [Range(1, 7, ErrorMessage = "El día de la semana no es válido.")]
    public int DiaSemana { get; set; }

    [Required(ErrorMessage = "Indique la hora de apertura.")]
    public string HoraInicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique la hora de cierre.")]
    public string HoraFin { get; set; } = string.Empty;
}

public sealed class GuardarHorarioViewModel
{
    public List<FranjaHorarioViewModel> Franjas { get; set; } = new();
}

public sealed class AgregarDiaNoLaboralViewModel
{
    [Required(ErrorMessage = "Seleccione la fecha.")]
    public DateOnly? Fecha { get; set; }

    [Required(ErrorMessage = "Indique el motivo.")]
    [StringLength(150, ErrorMessage = "El motivo no puede pasar de 150 caracteres.")]
    public string Motivo { get; set; } = string.Empty;

    public bool EsFeriado { get; set; }
}

public sealed class GuardarTipoSesionViewModel
{
    [Required(ErrorMessage = "Indique el nombre del tipo de sesión.")]
    [StringLength(50, ErrorMessage = "El nombre no puede pasar de 50 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(15, 240, ErrorMessage = "La duración debe estar entre 15 y 240 minutos.")]
    public int DuracionMinutos { get; set; }
}

public sealed class DatosConsultorioViewModel
{
    [Required(ErrorMessage = "Indique el nombre del consultorio.")]
    [StringLength(150)]
    public string NombreConsultorio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el correo de contacto.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string CorreoContacto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el teléfono de contacto.")]
    public string TelefonoContacto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique la dirección.")]
    [StringLength(255)]
    public string DireccionConsultorio { get; set; } = string.Empty;

    [Range(1, 72, ErrorMessage = "El recordatorio debe enviarse entre 1 y 72 horas antes.")]
    public int HorasRecordatorioCita { get; set; }
}