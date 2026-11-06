namespace MenteActiva.Api.Repositories;

// Solo las dos escrituras de estado del encargado. El resto de las operaciones
// de clientes siguen dentro de ClientesController con su propia conexion: pasar
// las diez a capas es un trabajo aparte y esta anotado como deuda (RG-117).
//
// Estas dos bajaron aqui porque las dos lanzan reglas de negocio con
// SIGNAL 45000 ("ya esta activo", "ya esta inactivo", "no existe") y
// RepositorioBase las convierte en ReglaNegocioException, que el manejador de
// excepciones traduce a 400 con su mensaje. Escritas en el controlador con
// Dapper suelto habria que armar a mano cada uno de esos errores.
public interface IRepositorioEncargado
{
    // Pone Activo = 0 y arrastra a los estudiantes que se quedan sin ningun
    // encargado activo. No toca el estado comercial: la reactivacion lo
    // necesita para devolverlo.
    Task DesactivarAsync(int idUsuarioAccion, int idEncargado, CancellationToken cancelacion);

    // La vuelta: Activo = 1, el estado comercial que tenia y los estudiantes
    // que cayeron con el.
    Task ReactivarAsync(int idUsuarioAccion, int idEncargado, CancellationToken cancelacion);
}
