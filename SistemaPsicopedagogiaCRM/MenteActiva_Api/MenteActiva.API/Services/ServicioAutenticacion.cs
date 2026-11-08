using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;
using Microsoft.AspNetCore.Identity;

namespace MenteActiva.Api.Services;

// Aqui viven las reglas del acceso. El hash se calcula y se compara en la
// aplicacion, nunca en la base el conteo de intentos y el bloqueo a los cinco
// los aplica el procedimiento para que ningun modulo pueda saltarselos.
public class ServicioAutenticacion : IServicioAutenticacion
{
    private const int EstadoActivo = 1;
    private const string MensajeCredencialInvalida = "Correo o contraseña incorrectos.";

    private readonly IRepositorioUsuario _repositorio;
    private readonly IGeneradorTokenAcceso _generador;
    private readonly IPasswordHasher<UsuarioAutenticado> _hasher;
    private readonly IServicioUsuarios _usuarios;
    private readonly ILogger<ServicioAutenticacion> _registro;

    public ServicioAutenticacion(
        IRepositorioUsuario repositorio,
        IGeneradorTokenAcceso generador,
        IPasswordHasher<UsuarioAutenticado> hasher,
        IServicioUsuarios usuarios,
        ILogger<ServicioAutenticacion> registro)
    {
        _repositorio = repositorio;
        _generador = generador;
        _hasher = hasher;
        _usuarios = usuarios;
        _registro = registro;
    }

    public async Task<ResultadoInicioSesion> IniciarSesionAsync(LoginRequest peticion)
    {
        var usuario = await _repositorio.ObtenerPorCorreoAsync(peticion.Correo);

        if (usuario is null || !ContrasenaCoincide(usuario, peticion.Contrasena))
        {
            // Suma el intento fallido incluso si el correo no existe, para que el
            // tiempo de respuesta no delate cuales cuentas estan registradas.
            await _repositorio.RegistrarFalloAsync(peticion.Correo);
            return new ResultadoInicioSesion(false, MensajeCredencialInvalida, null);
        }

        // El estado solo se revela cuando la contrasena ya era correcta.
        if (usuario.IdEstadoUsuario != EstadoActivo)
        {
            return new ResultadoInicioSesion(false, MensajeSegunEstado(usuario), null);
        }

        if (string.IsNullOrWhiteSpace(usuario.Rol))
        {
            return new ResultadoInicioSesion(
                false, "La cuenta no tiene un rol asignado. Contacte al administrador.", null);
        }

        await _repositorio.RegistrarIngresoAsync(usuario.IdUsuario);

        var token = _generador.Generar(usuario);

        return new ResultadoInicioSesion(true, "Ingreso correcto.", new LoginResponse
        {
            Token = token.Token,
            Expira = token.Expira,
            IdUsuario = usuario.IdUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Correo = usuario.Correo,
            Rol = usuario.Rol,
            IdEncargado = usuario.IdEncargado
        });
    }

    // El enlace y el correo los arma ServicioUsuarios, que ya resuelve los tres
    // pasos juntos y elige el texto segun el estado de la cuenta. Lo que no puede
    // salir de aqui es una excepcion: quien llama responde siempre lo mismo, y una
    // cuenta inactiva o sin correo no puede delatarse fallando.
    public async Task<string?> SolicitarRestablecimientoAsync(string correo)
    {
        var usuario = await _repositorio.ObtenerPorCorreoAsync(correo);

        if (usuario is null)
        {
            return null;
        }

        try
        {
            var resultado = await _usuarios.ReenviarEnlaceAsync(usuario.IdUsuario);

            if (!resultado.CorreoEnviado)
            {
                // El enlace quedo guardado igual, asi que el panel lo puede reenviar.
                _registro.LogWarning(
                    "El enlace del usuario {IdUsuario} se genero pero el correo no salio.",
                    usuario.IdUsuario);
            }

            return resultado.Token;
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "No se pudo generar el enlace de restablecimiento del usuario {IdUsuario}.",
                usuario.IdUsuario);

            return null;
        }
    }

    public async Task<int> RestablecerContrasenaAsync(RestablecerRequest peticion)
    {
        var hash = _hasher.HashPassword(new UsuarioAutenticado(), peticion.ContrasenaNueva);

        return await _repositorio.ConsumirTokenAsync(peticion.Token, hash);
    }

    private bool ContrasenaCoincide(UsuarioAutenticado usuario, string contrasena)
    {
        try
        {
            var resultado = _hasher.VerifyHashedPassword(
                usuario, usuario.ContrasenaHash, contrasena);

            return resultado != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Los usuarios invitados llevan una marca en vez de un hash real.
            // No es un error simplemente todavia no tienen contrasena.
            return false;
        }
    }

    private static string MensajeSegunEstado(UsuarioAutenticado usuario) => usuario.IdEstadoUsuario switch
    {
        2 => "La cuenta esta inactiva. Contacte al administrador.",
        3 => "La cuenta esta bloqueada por intentos fallidos. Restablezca su contraseña.",
        4 => "La cuenta todavia no se ha activado. Revise el enlace de invitacion.",
        _ => MensajeCredencialInvalida
    };
}
