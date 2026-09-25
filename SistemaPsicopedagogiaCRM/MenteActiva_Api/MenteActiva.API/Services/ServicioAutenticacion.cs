using System.Security.Cryptography;
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
    private const int HorasVigenciaEnlace = 1;
    private const string MensajeCredencialInvalida = "Correo o contraseña incorrectos.";

    private readonly IRepositorioUsuario _repositorio;
    private readonly IGeneradorTokenAcceso _generador;
    private readonly IPasswordHasher<UsuarioAutenticado> _hasher;

    public ServicioAutenticacion(
        IRepositorioUsuario repositorio,
        IGeneradorTokenAcceso generador,
        IPasswordHasher<UsuarioAutenticado> hasher)
    {
        _repositorio = repositorio;
        _generador = generador;
        _hasher = hasher;
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

    public async Task<string?> SolicitarRestablecimientoAsync(string correo)
    {
        var usuario = await _repositorio.ObtenerPorCorreoAsync(correo);

        if (usuario is null)
        {
            return null;
        }

        var token = GenerarTextoToken();

        await _repositorio.GenerarTokenAsync(usuario.IdUsuario, token, HorasVigenciaEnlace);

        return token;
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

    private static string GenerarTextoToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
