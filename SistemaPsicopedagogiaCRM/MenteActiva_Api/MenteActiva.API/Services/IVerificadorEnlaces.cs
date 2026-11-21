using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IVerificadorEnlaces
{
    // Nunca lanza por un enlace caido: eso es un resultado, Disponible = false
    // con el motivo. Solo deja pasar la cancelacion de la peticion.
    Task<VerificacionEnlaceResponse> VerificarAsync(string url, CancellationToken cancelacion);
}
