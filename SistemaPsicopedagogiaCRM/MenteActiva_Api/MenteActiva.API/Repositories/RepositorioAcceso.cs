using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioAcceso
{
    Task RegistrarAsync(
        int? idUsuario, string correo, string resultado, string? ip, string? navegador,
        CancellationToken cancelacion = default);

    Task<IReadOnlyList<AccesoUsuarioResponse>> ListarPorUsuarioAsync(
        int idUsuario, int limite, CancellationToken cancelacion);

    Task<IReadOnlyList<AccesoBitacoraResponse>> ListarAsync(
        FiltroAccesosRequest filtro, int limite, CancellationToken cancelacion);
}

// Bitacora de inicios de sesion. Solo llama SPs.
public sealed class RepositorioAcceso : RepositorioBase, IRepositorioAcceso
{
    public RepositorioAcceso(IConfiguration config)
        : base(config)
    {
    }

    public Task RegistrarAsync(
        int? idUsuario, string correo, string resultado, string? ip, string? navegador,
        CancellationToken cancelacion = default)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        // Recortados aqui: un texto mas largo que el parametro de la SP da error
        // en modo estricto y el intento se perderia
        parametros.Add("p_Correo", Recortar(correo, 150));
        parametros.Add("p_Resultado", resultado);
        parametros.Add("p_Ip", Recortar(ip, 45));
        parametros.Add("p_Navegador", Recortar(navegador, 300));

        return EjecutarAsync("SP_Acceso_Registrar", parametros, cancelacion);
    }

    private static string? Recortar(string? texto, int largo)
        => texto is null ? null : (texto.Length <= largo ? texto : texto[..largo]);

    public Task<IReadOnlyList<AccesoUsuarioResponse>> ListarPorUsuarioAsync(
        int idUsuario, int limite, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_Limite", limite);

        return ConsultarAsync<AccesoUsuarioResponse>("SP_Acceso_ListarPorUsuario", parametros, cancelacion);
    }

    public Task<IReadOnlyList<AccesoBitacoraResponse>> ListarAsync(
        FiltroAccesosRequest filtro, int limite, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Tipo", filtro.Tipo);
        parametros.Add("p_Resultado", filtro.Resultado);
        parametros.Add("p_Desde", filtro.Desde?.ToDateTime(TimeOnly.MinValue));
        parametros.Add("p_Hasta", filtro.Hasta?.ToDateTime(TimeOnly.MinValue));
        parametros.Add("p_Texto", filtro.Texto);
        parametros.Add("p_Limite", limite);

        return ConsultarAsync<AccesoBitacoraResponse>("SP_Acceso_Listar", parametros, cancelacion);
    }
}
