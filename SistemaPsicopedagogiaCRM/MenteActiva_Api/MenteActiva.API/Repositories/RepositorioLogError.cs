using Dapper;
using MenteActiva.Api.Models;
using MySqlConnector;

namespace MenteActiva.Api.Repositories;

// El unico repositorio con SQL en linea y sin procedimiento, y es a proposito:
// un procedimiento llamado desde la peticion que fallo correria dentro de su
// transaccion y el rollback se llevaria el registro. Por eso abre una conexion
// propia, que no es la de la operacion que se cayo.
//
// Tampoco hereda de RepositorioBase: esa clase solo sabe llamar procedimientos
// y traducir los SIGNAL 45000 a reglas de negocio, y aqui no hay ninguno.
public sealed class RepositorioLogError : IRepositorioLogError
{
    // Con la base caida el log no puede quedarse esperando los 15 segundos que
    // MySqlConnector trae por omision, porque la respuesta al usuario va detras
    private const uint SegundosDeEspera = 3;

    private const string Insercion =
        "INSERT INTO TB_LOG_ERROR " +
        "(Nivel, Origen, Mensaje, TipoExcepcion, StackTrace, " +
        " UrlSolicitada, MetodoHttp, IdUsuario, DireccionIp, NavegadorAgente) " +
        "VALUES " +
        "(@Nivel, @Origen, @Mensaje, @TipoExcepcion, @StackTrace, " +
        " @UrlSolicitada, @MetodoHttp, @IdUsuario, @DireccionIp, @NavegadorAgente);";

    private readonly string _cadenaConexion;

    public RepositorioLogError(IConfiguration config)
    {
        var cadena = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexion DefaultConnection en la configuracion.");

        _cadenaConexion = new MySqlConnectionStringBuilder(cadena)
        {
            ConnectionTimeout = SegundosDeEspera,
            DefaultCommandTimeout = (int)SegundosDeEspera
        }.ConnectionString;
    }

    public async Task GuardarAsync(ErrorAplicacion error, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        await conexion.ExecuteAsync(
            new CommandDefinition(Insercion, error, cancellationToken: cancelacion));
    }
}
