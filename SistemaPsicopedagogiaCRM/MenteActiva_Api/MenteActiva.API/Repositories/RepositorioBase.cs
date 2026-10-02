using System.Data;
using Dapper;
using MenteActiva.Api.Infraestructura;
using MySqlConnector;

namespace MenteActiva.Api.Repositories;

// Plomeria comun de los repositorios: abrir conexion, llamar la SP y traducir
// los SIGNAL '45000' a ReglaNegocioException. Asi no se copia en cada repositorio.
public abstract class RepositorioBase
{
    private const string ErrorReglaDeNegocio = "45000";

    private readonly string _cadenaConexion;

    protected RepositorioBase(IConfiguration config)
    {
        _cadenaConexion = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexion DefaultConnection en la configuracion.");
    }

    protected async Task<IReadOnlyList<T>> ConsultarAsync<T>(
        string procedimiento, DynamicParameters? parametros, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        var filas = await TraducirErroresAsync(() =>
            conexion.QueryAsync<T>(Comando(procedimiento, parametros, cancelacion)));

        return filas.AsList();
    }

    protected async Task EjecutarAsync(
        string procedimiento, DynamicParameters parametros, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        await TraducirErroresAsync(() =>
            conexion.ExecuteAsync(Comando(procedimiento, parametros, cancelacion)));
    }

    // Para las SPs que terminan con un SELECT del id creado
    protected async Task<int> EscalarAsync(
        string procedimiento, DynamicParameters parametros, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        return await TraducirErroresAsync(() =>
            conexion.ExecuteScalarAsync<int>(Comando(procedimiento, parametros, cancelacion)));
    }

    private static CommandDefinition Comando(
        string procedimiento, DynamicParameters? parametros, CancellationToken cancelacion)
        => new(procedimiento, parametros, commandType: CommandType.StoredProcedure, cancellationToken: cancelacion);

    private static async Task<T> TraducirErroresAsync<T>(Func<Task<T>> operacion)
    {
        try
        {
            return await operacion();
        }
        catch (MySqlException ex) when (ex.SqlState == ErrorReglaDeNegocio)
        {
            throw new ReglaNegocioException(ex.Message, ex);
        }
    }
}