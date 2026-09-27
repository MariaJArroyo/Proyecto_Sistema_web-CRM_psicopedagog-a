using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioUsuarios
{
    Task<ResultadoInvitacion> InvitarInternoAsync(int idUsuarioAccion, CrearUsuarioRequest peticion);

    Task<ResultadoInvitacion> InvitarEncargadoAsync(int idUsuarioAccion, int idEncargado);

    Task<ResultadoInvitacion> ReenviarEnlaceAsync(int idUsuario);

    Task<UsuarioDetalleResponse?> ObtenerAsync(int idUsuario);

    Task<IEnumerable<UsuarioResponse>> ListarAsync(
        string? busqueda, int? idEstadoUsuario, bool? soloInternos, int pagina, int tamanoPagina);

    Task<IEnumerable<UsuarioExternoResponse>> ListarExternosAsync(string? busqueda);

    Task AsignarRolAsync(int idUsuarioAccion, int idUsuario, int idRol);

    Task EditarAsync(int idUsuarioAccion, int idUsuario, EditarUsuarioRequest peticion);

    Task CambiarEstadoAsync(int idUsuarioAccion, int idUsuario, int idEstadoUsuario);

    Task SuspenderAccesoExternoAsync(int idUsuarioAccion, int idUsuario);

    Task ReactivarAccesoExternoAsync(int idUsuarioAccion, int idUsuario);
}

// CorreoEnviado en falso no es un fallo de la operacion: la cuenta y el enlace
// quedaron guardados, lo que no salio fue el mensaje.
public record ResultadoInvitacion(int IdUsuario, string Token, bool CorreoEnviado, string? ErrorCorreo);
