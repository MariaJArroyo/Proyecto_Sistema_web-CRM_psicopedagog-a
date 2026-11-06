using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Las reglas viven en los procedimientos, que son el unico lugar por donde se
// escribe: que el encargado exista, que no este ya en el estado al que se lo
// quiere llevar, y la cascada de los estudiantes. Todas salen como SIGNAL 45000
// y llegan aqui ya convertidas en ReglaNegocioException.
//
// Lo que se queda en esta capa es el guardia del id, que no es una regla de
// negocio sino evitar un viaje a la base con un valor que no puede existir.
public sealed class ServicioEncargado : IServicioEncargado
{
    private readonly IRepositorioEncargado _repositorio;

    public ServicioEncargado(IRepositorioEncargado repositorio)
    {
        _repositorio = repositorio;
    }

    public Task DesactivarAsync(int idUsuarioAccion, int idEncargado, CancellationToken cancelacion)
    {
        ValidarId(idEncargado);

        return _repositorio.DesactivarAsync(idUsuarioAccion, idEncargado, cancelacion);
    }

    public Task ReactivarAsync(int idUsuarioAccion, int idEncargado, CancellationToken cancelacion)
    {
        ValidarId(idEncargado);

        return _repositorio.ReactivarAsync(idUsuarioAccion, idEncargado, cancelacion);
    }

    private static void ValidarId(int idEncargado)
    {
        if (idEncargado <= 0)
        {
            throw new ReglaNegocioException("El encargado indicado no existe.");
        }
    }
}
