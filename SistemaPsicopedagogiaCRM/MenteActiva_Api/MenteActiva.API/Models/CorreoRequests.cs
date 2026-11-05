using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// Un destinatario por envio (D-F). El estudiante es opcional y solo hace falta
// cuando el texto usa [Estudiante]: sin el, ese marcador queda sin resolver y el
// envio se niega, que es mas claro que mandar un correo con un hueco.
//
// La cita tambien es opcional y es la que resuelve [Fecha], [Hora] y [Motivo].
// Solo tiene sentido junto con el estudiante, porque la cita es de el: mandarla
// sola se rechaza en el servicio.
//
// Cuerpo se corta en 1000 porque es el ancho de TB_NOTIFICACION.Cuerpo: si aqui
// fuera mas ancho, el error saldria de MySQL como 500 en vez de como mensaje.
public sealed record EnviarCorreoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un destinatario.")]
    public int IdEncargado { get; init; }

    public int? IdEstudiante { get; init; }

    public int? IdCita { get; init; }

    [Required(ErrorMessage = "Indique el asunto del correo.")]
    [StringLength(200, ErrorMessage = "El asunto no puede pasar de 200 caracteres.")]
    public string Asunto { get; init; } = string.Empty;

    [Required(ErrorMessage = "Indique el mensaje del correo.")]
    [StringLength(1000, ErrorMessage = "El mensaje no puede pasar de 1000 caracteres.")]
    public string Cuerpo { get; init; } = string.Empty;
}
