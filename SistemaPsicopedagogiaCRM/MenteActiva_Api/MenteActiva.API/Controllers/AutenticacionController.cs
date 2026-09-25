using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
[AllowAnonymous]
public class AutenticacionController : ControllerBase
{
    // Las reglas que rechaza un procedimiento llegan con este numero de error
    private const int ErrorReglaDeNegocio = 1644;

    private readonly IServicioAutenticacion _servicio;
    private readonly IWebHostEnvironment _entorno;

    public AutenticacionController(IServicioAutenticacion servicio, IWebHostEnvironment entorno)
    {
        _servicio = servicio;
        _entorno = entorno;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest peticion)
    {
        try
        {
            var resultado = await _servicio.IniciarSesionAsync(peticion);

            if (!resultado.Exito)
            {
                return Unauthorized(new { mensaje = resultado.Mensaje });
            }

            return Ok(resultado.Datos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al iniciar sesion.",
                detalle = ex.Message
            });
        }
    }

    // Siempre responde igual, exista o no la cuenta. Si distinguiera, cualquiera
    // podria averiguar que correos estan registrados.
    [HttpPost("solicitar-restablecimiento")]
    public async Task<IActionResult> SolicitarRestablecimiento(
        [FromBody] SolicitarRestablecimientoRequest peticion)
    {
        try
        {
            var token = await _servicio.SolicitarRestablecimientoAsync(peticion.Correo);

            var respuesta = new Dictionary<string, object?>
            {
                ["mensaje"] = "Si el correo esta registrado, recibira un enlace en los proximos minutos."
            };

            // Mientras no haya envio de correo, el enlace se devuelve aqui para
            // poder probar el flujo. Fuera de desarrollo nunca sale.
            if (_entorno.IsDevelopment() && token is not null)
            {
                respuesta["token"] = token;
            }

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al solicitar el restablecimiento.",
                detalle = ex.Message
            });
        }
    }

    [HttpPost("restablecer")]
    public Task<IActionResult> Restablecer([FromBody] RestablecerRequest peticion)
        => DefinirContrasena(peticion);

    // Activar una invitacion y restablecer una contrasena hacen hoy lo mismo:
    // consumir el token y guardar la contrasena nueva. Se dejan separados porque
    // la Web los presenta distinto y porque van a divergir cuando el token lleve
    // su tipo.
    [HttpPost("activar")]
    public Task<IActionResult> Activar([FromBody] RestablecerRequest peticion)
        => DefinirContrasena(peticion);

    private async Task<IActionResult> DefinirContrasena(RestablecerRequest peticion)
    {
        try
        {
            var idUsuario = await _servicio.RestablecerContrasenaAsync(peticion);

            return Ok(new
            {
                mensaje = "La contraseña quedo guardada. Ya puede iniciar sesion.",
                idUsuario
            });
        }
        catch (MySqlException ex) when (ex.Number == ErrorReglaDeNegocio)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al guardar la contraseña.",
                detalle = ex.Message
            });
        }
    }
}
