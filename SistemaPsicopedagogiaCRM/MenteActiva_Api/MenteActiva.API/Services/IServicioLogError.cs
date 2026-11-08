using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

// Guardar un fallo en TB_LOG_ERROR. Lo llama el manejador de excepciones.
public interface IServicioLogError
{
    // Recorta los textos al largo de las columnas y guarda la fila.
    //
    // No lanza nunca: si la base no responde, el fallo del log queda en el
    // registro del servidor y la peticion sigue su curso. Un log caido no puede
    // tumbar la respuesta al usuario.
    Task RegistrarAsync(ErrorAplicacion error, CancellationToken cancelacion);
}
