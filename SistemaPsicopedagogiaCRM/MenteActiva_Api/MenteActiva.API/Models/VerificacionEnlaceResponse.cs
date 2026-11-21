namespace MenteActiva.Api.Models;

// Resultado de comprobar un enlace. Responde 200 aunque el enlace este caido:
// que no funcione es la respuesta, no un error del API. La pantalla abre el
// enlace si Disponible es true y si no muestra Mensaje.
public sealed class VerificacionEnlaceResponse
{
    public bool Disponible { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
