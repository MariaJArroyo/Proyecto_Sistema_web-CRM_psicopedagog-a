namespace MenteActiva.Api.Services;

public interface IServicioEncargado
{
    Task DesactivarAsync(int idUsuarioAccion, int idEncargado, CancellationToken cancelacion);

    Task ReactivarAsync(int idUsuarioAccion, int idEncargado, CancellationToken cancelacion);
}
