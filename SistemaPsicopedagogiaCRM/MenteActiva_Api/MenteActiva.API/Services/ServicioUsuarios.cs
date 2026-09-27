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
    private const int HorasVigenciaRestablecimiento = 1;

    private const int EstadoActivo = 1;
    private const int EstadoInactivo = 2;
    private const int EstadoPendiente = 4;

    private readonly IRepositorioUsuario _repositorio;
    private readonly IServicioCorreo _correo;
    private readonly ILogger<ServicioUsuarios> _registro;
    private readonly string _urlBaseWeb;

    public ServicioUsuarios(
        IRepositorioUsuario repositorio,
        IServicioCorreo correo,
        ILogger<ServicioUsuarios> registro,
        IConfiguration config)
    {
        _repositorio = repositorio;
        _correo = correo;
        _registro = registro;
        _urlBaseWeb = (config["Correo:UrlBaseWeb"] ?? "https://localhost:7202").TrimEnd('/');
    }

    public async Task<ResultadoInvitacion> InvitarInternoAsync(
        int idUsuarioAccion, CrearUsuarioRequest peticion)
    {
        var idUsuario = await _repositorio.CrearAsync(
            idUsuarioAccion, peticion.NombreCompleto, peticion.Correo, peticion.IdRol);

        return await GenerarYEnviarAsync(
            idUsuario, peticion.NombreCompleto, peticion.Correo, esInvitacion: true);
    }

    public async Task<ResultadoInvitacion> InvitarEncargadoAsync(int idUsuarioAccion, int idEncargado)
    {
        var idUsuario = await _repositorio.CrearExternoAsync(idUsuarioAccion, idEncargado);

        // El nombre y el correo los tomo el procedimiento del encargado, asi que
        // se vuelven a leer de la cuenta recien creada
        var creado = await _repositorio.ObtenerPorIdAsync(idUsuario);

        return await GenerarYEnviarAsync(
            idUsuario,
            creado?.NombreCompleto ?? "estimado encargado",
            creado?.Correo ?? string.Empty,
            esInvitacion: true);
    }

    // Vuelve a mandar el enlace de una cuenta que ya existe. Si todavia no se
    // activo va el texto de invitacion; si ya tiene contrasena, el de
    // restablecimiento. El correo y el nombre se leen de la base: nunca se
    // aceptan del navegador, o cualquiera podria desviar el enlace.
    public async Task<ResultadoInvitacion> ReenviarEnlaceAsync(int idUsuario)
    {
        var usuario = await _repositorio.ObtenerPorIdAsync(idUsuario)
            ?? throw new InvalidOperationException("El usuario indicado no existe.");

        if (usuario.IdEstadoUsuario == EstadoInactivo)
        {
            throw new InvalidOperationException(
                "La cuenta esta inactiva. Reactivela antes de mandar el enlace.");
        }

        if (string.IsNullOrWhiteSpace(usuario.Correo))
        {
            throw new InvalidOperationException("La cuenta no tiene correo registrado.");
        }

        return await GenerarYEnviarAsync(
            usuario.IdUsuario,
            usuario.NombreCompleto,
            usuario.Correo,
            esInvitacion: usuario.IdEstadoUsuario == EstadoPendiente);
    }

    public Task<UsuarioDetalleResponse?> ObtenerAsync(int idUsuario)
        => _repositorio.ObtenerPorIdAsync(idUsuario);

    public Task<IEnumerable<UsuarioResponse>> ListarAsync(
        string? busqueda, int? idEstadoUsuario, bool? soloInternos, int pagina, int tamanoPagina)
        => _repositorio.ListarAsync(busqueda, idEstadoUsuario, soloInternos, pagina, tamanoPagina);

    public Task<IEnumerable<UsuarioExternoResponse>> ListarExternosAsync(string? busqueda)
        => _repositorio.ListarExternosAsync(busqueda);

    // El procedimiento valida que el rol exista y este activo, borra el rol
    // anterior y deja rastro en la bitacora, todo en una transaccion.
    public Task AsignarRolAsync(int idUsuarioAccion, int idUsuario, int idRol)
        => _repositorio.AsignarRolAsync(idUsuarioAccion, idUsuario, idRol);

    public Task EditarAsync(int idUsuarioAccion, int idUsuario, EditarUsuarioRequest peticion)
        => _repositorio.EditarAsync(
            idUsuarioAccion, idUsuario, peticion.NombreCompleto, peticion.Correo, peticion.IdRol);

    public Task CambiarEstadoAsync(int idUsuarioAccion, int idUsuario, int idEstadoUsuario)
        => _repositorio.CambiarEstadoAsync(idUsuarioAccion, idUsuario, idEstadoUsuario);

    public Task SuspenderAccesoExternoAsync(int idUsuarioAccion, int idUsuario)
        => _repositorio.SuspenderAccesoExternoAsync(idUsuarioAccion, idUsuario);

    public Task ReactivarAccesoExternoAsync(int idUsuarioAccion, int idUsuario)
        => _repositorio.CambiarEstadoAsync(idUsuarioAccion, idUsuario, EstadoActivo);

    // Si el envio falla, la cuenta y el enlace igual quedan guardados y se
    // avisa que el correo no salio. Tirar la excepcion aqui dejaba cuentas
    // creadas que la pantalla reportaba como fallidas y no habia como
    // recuperarlas: para eso esta el reenvio.
    private async Task<ResultadoInvitacion> GenerarYEnviarAsync(
        int idUsuario, string nombre, string correo, bool esInvitacion)
    {
        var token = TokenEnlace.Generar();
        var horas = esInvitacion ? HorasVigenciaInvitacion : HorasVigenciaRestablecimiento;

        await _repositorio.GenerarTokenAsync(idUsuario, token, horas);

        if (string.IsNullOrWhiteSpace(correo))
        {
            return new ResultadoInvitacion(idUsuario, token, false, "La cuenta no tiene correo registrado.");
        }

        try
        {
            await _correo.EnviarAsync(
                correo,
                esInvitacion ? MensajesCuenta.AsuntoInvitacion : MensajesCuenta.AsuntoRecuperacion,
                esInvitacion
                    ? MensajesCuenta.Invitacion(_urlBaseWeb, nombre, token, horas)
                    : MensajesCuenta.Recuperacion(_urlBaseWeb, nombre, token, horas));

            return new ResultadoInvitacion(idUsuario, token, true, null);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "No se pudo enviar el correo a {Destinatario}. La cuenta {IdUsuario} quedo creada y el enlace sigue valido.",
                correo, idUsuario);

            return new ResultadoInvitacion(idUsuario, token, false, ex.Message);
        }
    }
}
