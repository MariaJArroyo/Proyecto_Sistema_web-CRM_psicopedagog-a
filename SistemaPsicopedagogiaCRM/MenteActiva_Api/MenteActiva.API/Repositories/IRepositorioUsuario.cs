using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioUsuario
{
    Task<UsuarioAutenticado?> ObtenerPorCorreoAsync(string correo);

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
}
