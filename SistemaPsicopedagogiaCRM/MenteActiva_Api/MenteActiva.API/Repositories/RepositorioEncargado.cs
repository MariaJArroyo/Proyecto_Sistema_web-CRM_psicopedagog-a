using Dapper;

namespace MenteActiva.Api.Repositories;

public sealed class RepositorioEncargado : RepositorioBase, IRepositorioEncargado
{
    public RepositorioEncargado(IConfiguration config)
        : base(config)
    {
    }

    public Task DesactivarAsync(int idUsuarioAccion, int idEncargado, CancellationToken cancelacion)
        => EjecutarAsync("SP_DesactivarCliente_CRM", Parametros(idUsuarioAccion, idEncargado), cancelacion);

    public Task ReactivarAsync(int idUsuarioAccion, int idEncargado, CancellationToken cancelacion)
        => EjecutarAsync("SP_Encargado_Reactivar", Parametros(idUsuarioAccion, idEncargado), cancelacion);

    private static DynamicParameters Parametros(int idUsuarioAccion, int idEncargado)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEncargado", idEncargado);

        return parametros;
    }
}
