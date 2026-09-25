using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioAutenticacion
{
    Task<ResultadoInicioSesion> IniciarSesionAsync(LoginRequest peticion);

    // Devuelve el token generado, o null si el correo no tiene cuenta. Quien
    // llama nunca debe mostrar esa diferencia al usuario.
    Task<string?> SolicitarRestablecimientoAsync(string correo);

    Task<int> RestablecerContrasenaAsync(RestablecerRequest peticion);
}

public record ResultadoInicioSesion(bool Exito, string Mensaje, LoginResponse? Datos);
