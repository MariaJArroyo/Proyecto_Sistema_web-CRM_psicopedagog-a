using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioAutenticacion
{
    Task<ResultadoInicioSesion> IniciarSesionAsync(LoginRequest peticion);

    // Genera el enlace y manda el correo de recuperacion. Devuelve el token
    // generado, o null si el correo no tiene cuenta, si la cuenta esta inactiva o
    // si el enlace no se pudo guardar. Quien llama nunca debe mostrar ninguna de
    // esas diferencias al usuario.
    Task<string?> SolicitarRestablecimientoAsync(string correo);

    Task<int> RestablecerContrasenaAsync(RestablecerRequest peticion);
}

public record ResultadoInicioSesion(bool Exito, string Mensaje, LoginResponse? Datos);
