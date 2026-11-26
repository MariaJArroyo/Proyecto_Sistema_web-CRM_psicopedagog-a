using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// Sirve para las dos cosas: con IdHilo en nulo abre una conversacion nueva y
// hace falta asunto; con IdHilo responde, y el destinatario y el asunto se sacan
// del hilo en el procedimiento para que no se pueda colar un tercero.
public sealed record EnviarMensajeRequest
{
    public int? IdHilo { get; init; }

    // Solo para el personal que abre una conversacion con un encargado. Lo que
    // escribe un encargado va siempre a la bandeja del consultorio y este campo
    // se ignora.
    public int? IdUsuarioDestinatario { get; init; }

    [StringLength(200, ErrorMessage = "El asunto no puede pasar de 200 caracteres.")]
    public string? Asunto { get; init; }

    [Required(ErrorMessage = "Escriba el mensaje.")]
    [StringLength(4000, ErrorMessage = "El mensaje no puede pasar de 4000 caracteres.")]
    public string Cuerpo { get; init; } = string.Empty;
}

// Lista completa de roles que quedan con acceso a la bandeja del consultorio
// (1 Administrador, 2 Psicopedagoga, 3 Asistente). Lo que no viene, pierde el
// acceso. El procedimiento valida los ids y que quede al menos uno.
public sealed record GuardarAccesoMensajeriaRequest
{
    public List<int>? Roles { get; init; }
}
