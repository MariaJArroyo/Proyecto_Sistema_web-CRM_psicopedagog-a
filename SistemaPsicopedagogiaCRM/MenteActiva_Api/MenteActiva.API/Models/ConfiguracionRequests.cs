using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public sealed record FranjaHorarioRequest
{
    [Range(1, 7, ErrorMessage = "El día de la semana no es válido.")]
    public int DiaSemana { get; init; }

    // "HH:mm" (08:00, 13:30). El servicio la convierte y valida el rango.
    [Required(ErrorMessage = "Indique la hora de apertura.")]
    public string HoraInicio { get; init; } = string.Empty;

    [Required(ErrorMessage = "Indique la hora de cierre.")]
    public string HoraFin { get; init; } = string.Empty;
}

public sealed record GuardarHorarioRequest
{
    [Required(ErrorMessage = "Defina al menos una franja de atención.")]
    [MinLength(1, ErrorMessage = "Defina al menos una franja de atención.")]
    public List<FranjaHorarioRequest> Franjas { get; init; } = [];
}

public sealed record AgregarDiaNoLaboralRequest
{
    [Required(ErrorMessage = "Seleccione la fecha.")]
    public DateOnly? Fecha { get; init; }

    [Required(ErrorMessage = "Indique el motivo.")]
    [StringLength(150, ErrorMessage = "El motivo no puede pasar de 150 caracteres.")]
    public string Motivo { get; init; } = string.Empty;

    public bool EsFeriado { get; init; }
}

public sealed record GuardarTipoSesionRequest
{
    [Required(ErrorMessage = "Indique el nombre del tipo de sesión.")]
    [StringLength(50, ErrorMessage = "El nombre no puede pasar de 50 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [Range(15, 240, ErrorMessage = "La duración debe estar entre 15 y 240 minutos.")]
    public int DuracionMinutos { get; init; }

    // Vacio = sin precio (no se generan cobros automaticos de este tipo)
    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "El precio debe estar entre 0 y 10 000 000.")]
    public decimal? Precio { get; init; }
}
public sealed record GuardarDatosConsultorioRequest
{
    [Required(ErrorMessage = "Indique el nombre del consultorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede pasar de 150 caracteres.")]
    public string NombreConsultorio { get; init; } = string.Empty;

    [Required(ErrorMessage = "Indique el correo de contacto.")]
    [StringLength(150, ErrorMessage = "El correo no puede pasar de 150 caracteres.")]
    public string CorreoContacto { get; init; } = string.Empty;

    // Se aceptan espacios o guiones (2222-3333); el servicio deja solo los digitos
    [Required(ErrorMessage = "Indique el teléfono de contacto.")]
    public string TelefonoContacto { get; init; } = string.Empty;

    [Required(ErrorMessage = "Indique la dirección.")]
    [StringLength(255, ErrorMessage = "La dirección no puede pasar de 255 caracteres.")]
    public string DireccionConsultorio { get; init; } = string.Empty;

    [Range(1, 72, ErrorMessage = "El recordatorio debe enviarse entre 1 y 72 horas antes.")]
    public int HorasRecordatorioCita { get; init; }

    // Nullable: si la pantalla no lo manda, se conserva el valor guardado
    [Range(0, 90, ErrorMessage = "El plazo de pago debe estar entre 0 y 90 días (0 = no vence).")]
    public int? DiasVencimientoPago { get; init; }
}