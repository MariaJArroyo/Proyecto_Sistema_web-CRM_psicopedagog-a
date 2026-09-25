using System.Security.Claims;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private const int ErrorReglaDeNegocio = 1644;

    private readonly IServicioUsuarios _servicio;
    private readonly IWebHostEnvironment _entorno;

    public UsuariosController(IServicioUsuarios servicio, IWebHostEnvironment entorno)
    {
        _servicio = servicio;
        _entorno = entorno;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos(
        [FromQuery] string? busqueda = null,
        [FromQuery] int? idEstadoUsuario = null,
        [FromQuery] bool? soloInternos = true,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 25)
    {
        try
        {
            var usuarios = await _servicio.ListarAsync(
                busqueda, idEstadoUsuario, soloInternos, pagina, tamanoPagina);

            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al consultar los usuarios.",
                detalle = ex.Message
            });
        }
    }

    // Crea la cuenta sin contrasena y manda el enlace de activacion. La persona
    // define su propia contrasena; aqui nadie la escribe por ella.
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest peticion)
    {
        try
        {
            var resultado = await _servicio.InvitarInternoAsync(ObtenerIdUsuarioActual(), peticion);

            var respuesta = new Dictionary<string, object?>
            {
                ["mensaje"] = "Usuario creado. Se envió el enlace de activación.",
                ["idUsuario"] = resultado.IdUsuario
            };

            // Mientras no haya buzon, el enlace se devuelve para poder probar
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
                mensaje = "Error al crear el usuario.",
                detalle = ex.Message
            });
        }
    }

    // HU-M13-1: el administrador cambia el rol de un usuario. Un rol por
    // persona: el procedimiento borra el anterior antes de poner el nuevo.
    [HttpPut("{id:int}/rol")]
    public async Task<IActionResult> AsignarRol(int id, [FromBody] AsignarRolRequest peticion)
    {
        try
        {
            await _servicio.AsignarRolAsync(ObtenerIdUsuarioActual(), id, peticion.IdRol);

            return Ok(new { mensaje = "El rol quedó asignado." });
        }
        catch (MySqlException ex) when (ex.Number == ErrorReglaDeNegocio)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al asignar el rol.",
                detalle = ex.Message
            });
        }
    }

    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
