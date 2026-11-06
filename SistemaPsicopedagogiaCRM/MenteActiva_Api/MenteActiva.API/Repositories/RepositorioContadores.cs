using Dapper;

namespace MenteActiva.Api.Repositories;

public sealed class RepositorioContadores : RepositorioBase, IRepositorioContadores
{
    public RepositorioContadores(IConfiguration config)
        : base(config)
    {
    }

    public Task<int> EncargadosSinInvitarAsync(CancellationToken cancelacion)
        => EscalarAsync("SP_UsuarioExterno_ContarSinInvitar", new DynamicParameters(), cancelacion);

    public Task<int> NotificacionesFallidasAsync(int maxIntentos, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_MaxIntentos", maxIntentos);

        return EscalarAsync("SP_Notificacion_ContarFallidas", parametros, cancelacion);
    }

    public Task<int> CitasSinSesionAsync(DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_Ahora", ahora);

        return EscalarAsync("SP_Sesion_ContarCitasSinSesion", parametros, cancelacion);
    }

    public Task<int> PagosVencidosAsync(CancellationToken cancelacion)
        => EscalarAsync("SP_Pago_ContarVencidos", new DynamicParameters(), cancelacion);
}
