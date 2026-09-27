using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioUsuario
{
    Task<UsuarioAutenticado?> ObtenerPorCorreoAsync(string correo);

    Task<UsuarioDetalleResponse?> ObtenerPorIdAsync(int idUsuario);

    Task RegistrarIngresoAsync(int idUsuario);

    Task RegistrarFalloAsync(string correo);

    Task GenerarTokenAsync(int idUsuario, string token, int horasVigencia);

    Task<int> ConsumirTokenAsync(string token, string contrasenaHash);

    Task<int> CrearAsync(int idUsuarioAccion, string nombreCompleto, string correo, int idRol);

    Task<int> CrearExternoAsync(int idUsuarioAccion, int idEncargado);

    Task<IEnumerable<UsuarioResponse>> ListarAsync(
        string? busqueda, int? idEstadoUsuario, bool? soloInternos, int pagina, int tamanoPagina);

    Task<IEnumerable<UsuarioExternoResponse>> ListarExternosAsync(string? busqueda);

    Task AsignarRolAsync(int idUsuarioAccion, int idUsuario, int idRol);

    Task EditarAsync(int idUsuarioAccion, int idUsuario, string nombreCompleto, string correo, int idRol);

    Task CambiarEstadoAsync(int idUsuarioAccion, int idUsuario, int idEstadoUsuario);

    Task SuspenderAccesoExternoAsync(int idUsuarioAccion, int idUsuario);
}
