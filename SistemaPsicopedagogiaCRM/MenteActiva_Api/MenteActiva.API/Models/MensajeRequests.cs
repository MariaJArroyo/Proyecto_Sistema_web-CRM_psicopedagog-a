using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// Sirve para las dos cosas: con IdHilo en nulo abre una conversacion nueva y
// hacen falta destinatario y asunto; con IdHilo responde, y los dos se sacan del
// hilo en el procedimiento para que no se pueda colar una respuesta a un tercero.
public sealed record EnviarMensajeRequest
{
    public int? IdHilo { get; init; }

    public int? IdUsuarioDestinatario { get; init; }

    [StringLength(200, ErrorMessage = "El asunto no puede pasar de 200 caracteres.")]
    public string? Asunto { get; init; }

    [Required(ErrorMessage = "Escriba el mensaje.")]
    [StringLength(4000, ErrorMessage = "El mensaje no puede pasar de 4000 caracteres.")]
    public string Cuerpo { get; init; } = string.Empty;
}
