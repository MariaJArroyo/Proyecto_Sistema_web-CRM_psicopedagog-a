using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioUsuarios
{
    Task<ResultadoInvitacion> InvitarInternoAsync(int idUsuarioAccion, CrearUsuarioRequest peticion);

    Task<ResultadoInvitacion> InvitarEncargadoAsync(int idUsuarioAccion, int idEncargado);

    Task<IEnumerable<UsuarioResponse>> ListarAsync(
        string? busqueda, int? idEstadoUsuario, bool? soloInternos, int pagina, int tamanoPagina);

    Task<IEnumerable<UsuarioExternoResponse>> ListarExternosAsync(string? busqueda);

    Task AsignarRolAsync(int idUsuarioAccion, int idUsuario, int idRol);
}

public record ResultadoInvitacion(int IdUsuario, string Token);
