namespace MenteActiva.Api.Services;

public interface IServicioContadores
{
    Task<int> EncargadosSinInvitarAsync(CancellationToken cancelacion);

    Task<int> NotificacionesFallidasAsync(CancellationToken cancelacion);

    Task<int> CitasSinSesionAsync(CancellationToken cancelacion);

    Task<int> PagosVencidosAsync(CancellationToken cancelacion);
}
