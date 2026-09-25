using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Invitar a alguien son tres pasos que van siempre juntos: crear la cuenta sin
// contrasena, generar el enlace y mandarlo. Por eso viven en un solo metodo.
public class ServicioUsuarios : IServicioUsuarios
{
    // Mas holgado que el de recuperacion: una invitacion puede quedar sin abrir
    // un fin de semana entero
    private const int HorasVigenciaInvitacion = 48;

    private readonly IRepositorioUsuario _repositorio;
    private readonly IServicioCorreo _correo;
    private readonly string _urlBaseWeb;

    public ServicioUsuarios(
        IRepositorioUsuario repositorio,
        IServicioCorreo correo,
        IConfiguration config)
    {
        _repositorio = repositorio;
        _correo = correo;
        _urlBaseWeb = (config["Correo:UrlBaseWeb"] ?? "https://localhost:7202").TrimEnd('/');
    }

    public async Task<ResultadoInvitacion> InvitarInternoAsync(
        int idUsuarioAccion, CrearUsuarioRequest peticion)
    {
        var idUsuario = await _repositorio.CrearAsync(
            idUsuarioAccion, peticion.NombreCompleto, peticion.Correo, peticion.IdRol);

        var token = await GenerarYEnviarAsync(idUsuario, peticion.NombreCompleto, peticion.Correo);

        return new ResultadoInvitacion(idUsuario, token);
    }

    public async Task<ResultadoInvitacion> InvitarEncargadoAsync(int idUsuarioAccion, int idEncargado)
    {
        var idUsuario = await _repositorio.CrearExternoAsync(idUsuarioAccion, idEncargado);

        // El nombre y el correo los tomo el procedimiento del encargado, asi que
        // se vuelven a leer de la cuenta recien creada
        var externos = await _repositorio.ListarExternosAsync(null);
        var creado = externos.FirstOrDefault(e => e.IdUsuario == idUsuario);

        var token = await GenerarYEnviarAsync(
            idUsuario,
            creado?.Encargado ?? "estimado encargado",
            creado?.Correo ?? string.Empty);

        return new ResultadoInvitacion(idUsuario, token);
    }

    public Task<IEnumerable<UsuarioResponse>> ListarAsync(
        string? busqueda, int? idEstadoUsuario, bool? soloInternos, int pagina, int tamanoPagina)
        => _repositorio.ListarAsync(busqueda, idEstadoUsuario, soloInternos, pagina, tamanoPagina);

    public Task<IEnumerable<UsuarioExternoResponse>> ListarExternosAsync(string? busqueda)
        => _repositorio.ListarExternosAsync(busqueda);

    // El procedimiento valida que el rol exista y este activo, borra el rol
    // anterior y deja rastro en la bitacora, todo en una transaccion.
    public Task AsignarRolAsync(int idUsuarioAccion, int idUsuario, int idRol)
        => _repositorio.AsignarRolAsync(idUsuarioAccion, idUsuario, idRol);

    private async Task<string> GenerarYEnviarAsync(int idUsuario, string nombre, string correo)
    {
        var token = TokenEnlace.Generar();

        await _repositorio.GenerarTokenAsync(idUsuario, token, HorasVigenciaInvitacion);

        if (!string.IsNullOrWhiteSpace(correo))
        {
            await _correo.EnviarAsync(
                correo,
                MensajesCuenta.AsuntoInvitacion,
                MensajesCuenta.Invitacion(_urlBaseWeb, nombre, token, HorasVigenciaInvitacion));
        }

        return token;
    }
}
