using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Escritura del log tecnico de fallos. Es el unico repositorio que no pasa por
// un procedimiento almacenado, y la seccion 6 del archivo general dice por que:
// el log existe para registrar un error aunque la transaccion que lo provoco se
// revierta, asi que no puede vivir dentro de esa transaccion.
public interface IRepositorioLogError
{
    // Inserta la fila en TB_LOG_ERROR con su propia conexion. Puede lanzar si la
    // base no responde; quien decide que hacer con eso es el servicio.
    Task GuardarAsync(ErrorAplicacion error, CancellationToken cancelacion);
}
