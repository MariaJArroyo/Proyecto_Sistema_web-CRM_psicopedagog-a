using System.Security.Claims;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/usuarios-externos")]
[Authorize(Roles = "Administrador,Psicopedagoga")]
public class UsuariosExternosController : ControllerBase
{
    private const int ErrorReglaDeNegocio = 1644;

    private readonly IServicioUsuarios _servicio;
    private readonly IWebHostEnvironment _entorno;

    public UsuariosExternosController(IServicioUsuarios servicio, IWebHostEnvironment entorno)
    {
        _servicio = servicio;
        _entorno = entorno;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] string? busqueda = null)
    {
        try
        {
            var encargados = await _servicio.ListarExternosAsync(busqueda);

            return Ok(encargados);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al consultar los usuarios externos.",
                detalle = ex.Message
            });
        }
    }

    // Habilita el portal a un encargado que ya existe como cliente
    [HttpPost]
    public async Task<IActionResult> Invitar([FromBody] InvitarEncargadoRequest peticion)
    {
        try
        {
            var resultado = await _servicio.InvitarEncargadoAsync(
                ObtenerIdUsuarioActual(), peticion.IdEncargado);

            var respuesta = new Dictionary<string, object?>
            {
                ["mensaje"] = resultado.CorreoEnviado
                    ? "Se envió la invitación al portal."
                    : "La cuenta del portal quedó creada, pero el correo no salió. Use \"Reenviar invitación\".",
                ["idUsuario"] = resultado.IdUsuario,
                ["correoEnviado"] = resultado.CorreoEnviado
            };

            if (!resultado.CorreoEnviado)
            {
                respuesta["detalleCorreo"] = resultado.ErrorCorreo;
            }

            if (_entorno.IsDevelopment())
            {
                respuesta["token"] = resultado.Token;
            }

            return Ok(respuesta);
        }
        catch (MySqlException ex) when (ex.Number == ErrorReglaDeNegocio)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al invitar al encargado.",
                detalle = ex.Message
            });
        }
    }

    // El mismo reenvio que el de usuarios internos, pero abierto tambien a la
    // psicopedagoga: esta pantalla es suya y la de usuarios internos no.
    [HttpPost("{idUsuario:int}/invitacion")]
    public async Task<IActionResult> ReenviarInvitacion(int idUsuario)
    {
        try
        {
            var resultado = await _servicio.ReenviarEnlaceAsync(idUsuario);

            var respuesta = new Dictionary<string, object?>
            {
                ["mensaje"] = resultado.CorreoEnviado
                    ? "Se envió el enlace al correo del encargado."
                    : "No se pudo enviar el correo. El enlace quedó generado.",
                ["correoEnviado"] = resultado.CorreoEnviado
            };

            if (!resultado.CorreoEnviado)
            {
                respuesta["detalleCorreo"] = resultado.ErrorCorreo;
            }

            if (_entorno.IsDevelopment())
            {
                respuesta["token"] = resultado.Token;
            }

            return Ok(respuesta);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (MySqlException ex) when (ex.Number == ErrorReglaDeNegocio)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al reenviar el enlace.",
                detalle = ex.Message
            });
        }
    }

    // Suspender le quita el acceso al portal sin tocar al encargado, que sigue
    // siendo cliente. Reactivar lo devuelve a Activo.
    [HttpPut("{idUsuario:int}/acceso")]
    public async Task<IActionResult> CambiarAcceso(int idUsuario, [FromBody] CambiarAccesoRequest peticion)
    {
        try
        {
            if (peticion.Suspender)
            {
                await _servicio.SuspenderAccesoExternoAsync(ObtenerIdUsuarioActual(), idUsuario);

                return Ok(new { mensaje = "Se suspendió el acceso al portal." });
            }

            await _servicio.ReactivarAccesoExternoAsync(ObtenerIdUsuarioActual(), idUsuario);

            return Ok(new { mensaje = "Se reactivó el acceso al portal." });
        }
        catch (MySqlException ex) when (ex.Number == ErrorReglaDeNegocio)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al cambiar el acceso al portal.",
                detalle = ex.Message
            });
        }
    }

    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
