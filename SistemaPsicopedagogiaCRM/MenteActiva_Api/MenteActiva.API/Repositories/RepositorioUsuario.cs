using System.Data;
using Dapper;
using MenteActiva.Api.Models;
using MySqlConnector;

namespace MenteActiva.Api.Repositories;

// Lo unico que abre conexion y llama procedimientos. Aqui no hay reglas de
// negocio: si algo hay que decidir, se decide en el servicio.
public class RepositorioUsuario : IRepositorioUsuario
{
    private readonly string _cadenaConexion;

    public RepositorioUsuario(IConfiguration config)
    {
        _cadenaConexion = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexion DefaultConnection en la configuracion.");
    }

    public async Task<UsuarioAutenticado?> ObtenerPorCorreoAsync(string correo)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_Correo", correo);

        return await conexion.QueryFirstOrDefaultAsync<UsuarioAutenticado>(
            "SP_Usuario_ObtenerPorCorreo",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task RegistrarIngresoAsync(int idUsuario)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);

        await conexion.ExecuteAsync(
            "SP_Usuario_RegistrarIngreso",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task RegistrarFalloAsync(string correo)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_Correo", correo);

        await conexion.ExecuteAsync(
            "SP_Usuario_RegistrarFallo",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task GenerarTokenAsync(int idUsuario, string token, int horasVigencia)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_Token", token);
        parametros.Add("p_HorasVigencia", horasVigencia);

        await conexion.ExecuteAsync(
            "SP_Token_Generar",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> ConsumirTokenAsync(string token, string contrasenaHash)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_Token", token);
        parametros.Add("p_ContrasenaHash", contrasenaHash);

        return await conexion.ExecuteScalarAsync<int>(
            "SP_Token_Consumir",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> CrearAsync(int idUsuarioAccion, string nombreCompleto, string correo, int idRol)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_NombreCompleto", nombreCompleto);
        parametros.Add("p_Correo", correo);
        parametros.Add("p_IdRol", idRol);

        return await conexion.ExecuteScalarAsync<int>(
            "SP_Usuario_Crear",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> CrearExternoAsync(int idUsuarioAccion, int idEncargado)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEncargado", idEncargado);

        return await conexion.ExecuteScalarAsync<int>(
            "SP_UsuarioExterno_Crear",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<UsuarioResponse>> ListarAsync(
        string? busqueda, int? idEstadoUsuario, bool? soloInternos, int pagina, int tamanoPagina)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_Busqueda", busqueda);
        parametros.Add("p_IdEstadoUsuario", idEstadoUsuario);
        parametros.Add("p_SoloInternos", soloInternos);
        parametros.Add("p_Pagina", pagina);
        parametros.Add("p_TamanoPagina", tamanoPagina);

        return await conexion.QueryAsync<UsuarioResponse>(
            "SP_Usuario_Listar",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<UsuarioExternoResponse>> ListarExternosAsync(string? busqueda)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_Busqueda", busqueda);

        return await conexion.QueryAsync<UsuarioExternoResponse>(
            "SP_UsuarioExterno_Listar",
            parametros,
            commandType: CommandType.StoredProcedure);
    }

    public async Task AsignarRolAsync(int idUsuarioAccion, int idUsuario, int idRol)
    {
        using var conexion = new MySqlConnection(_cadenaConexion);

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdRol", idRol);

        await conexion.ExecuteAsync(
            "SP_Usuario_AsignarRol",
            parametros,
            commandType: CommandType.StoredProcedure);
    }
}
