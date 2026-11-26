using Dapper;
using MenteActiva.Api.Models;
using MySqlConnector;

namespace MenteActiva.Api.Repositories;

// Todo va por procedimiento salvo TieneAccesoAsync, que solo lee la funcion
// FN_Mensajeria_TieneAcceso y abre su propia conexion porque la base solo sabe
// llamar procedimientos (mismo caso que RepositorioMaterial).
public sealed class RepositorioMensaje : RepositorioBase, IRepositorioMensaje
{
    private readonly string _cadenaConexion;

    public RepositorioMensaje(IConfiguration config)
        : base(config)
    {
        _cadenaConexion = config.GetConnectionString("DefaultConnection")!;
    }

    public Task<IReadOnlyList<HiloResponse>> ListarHilosAsync(
        int idUsuario, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);

        // Se reutiliza el que ya existia desde antes del modulo 10
        return ConsultarAsync<HiloResponse>("SP_Comunicacion_ListarHilos", parametros, cancelacion);
    }

    public Task<IReadOnlyList<MensajeResponse>> AbrirHiloAsync(
        int idUsuario, int idHilo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdHilo", idHilo);

        return ConsultarAsync<MensajeResponse>("SP_Mensaje_AbrirHilo", parametros, cancelacion);
    }

    public Task<IReadOnlyList<DestinatarioMensajeResponse>> ListarDestinatariosAsync(
        int idUsuario, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);

        return ConsultarAsync<DestinatarioMensajeResponse>(
            "SP_Mensaje_ListarDestinatarios", parametros, cancelacion);
    }

    public Task<int> ContarSinLeerAsync(int idUsuario, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);

        return EscalarAsync("SP_Mensaje_ContarSinLeer", parametros, cancelacion);
    }

    public async Task<(int IdMensaje, int IdHilo)> EnviarAsync(
        int idUsuarioRemitente, int? idUsuarioDestinatario, int? idHilo,
        string? asunto, string cuerpo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioRemitente", idUsuarioRemitente);
        parametros.Add("p_IdUsuarioDestinatario", idUsuarioDestinatario);
        parametros.Add("p_IdMensajePadre", idHilo);
        parametros.Add("p_Asunto", asunto?.Trim());
        parametros.Add("p_Cuerpo", cuerpo.Trim());

        // El procedimiento cierra con SELECT del id del mensaje y del hilo
        var filas = await ConsultarAsync<MensajeEnviadoResponse>(
            "SP_Mensaje_Enviar", parametros, cancelacion);

        var fila = filas.FirstOrDefault()
            ?? throw new InvalidOperationException("SP_Mensaje_Enviar no devolvio el id.");

        return (fila.IdMensaje, fila.IdHilo);
    }

    public async Task<bool> TieneAccesoAsync(int idUsuario, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT FN_Mensajeria_TieneAcceso(@IdUsuario);",
            new { IdUsuario = idUsuario },
            cancellationToken: cancelacion));
    }

    public Task<IReadOnlyList<AccesoMensajeriaRolResponse>> ListarAccesoAsync(CancellationToken cancelacion)
        => ConsultarAsync<AccesoMensajeriaRolResponse>("SP_Mensajeria_ListarAcceso", null, cancelacion);

    public Task GuardarAccesoAsync(int idUsuarioAccion, string rolesJson, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_RolesJson", rolesJson);

        return EjecutarAsync("SP_Mensajeria_GuardarAcceso", parametros, cancelacion);
    }
}
