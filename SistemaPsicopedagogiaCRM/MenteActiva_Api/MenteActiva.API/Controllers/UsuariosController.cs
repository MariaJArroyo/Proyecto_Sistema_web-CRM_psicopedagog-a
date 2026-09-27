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
    private const int EstadoActivo = 1;

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

            return Ok(Describir(resultado,
                "Usuario creado. Se envió el enlace de activación.",
                "Usuario creado, pero el correo no salió. Use \"Reenviar invitación\" cuando el envío esté configurado."));
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        try
        {
            var usuario = await _servicio.ObtenerAsync(id);

            return usuario is null
                ? NotFound(new { mensaje = "El usuario indicado no existe." })
                : Ok(usuario);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al consultar el usuario.",
                detalle = ex.Message
            });
        }
    }

    // Nombre, correo y rol. Si le cambian el correo, el procedimiento anula los
    // enlaces vivos: se mandaron a la direccion anterior.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarUsuarioRequest peticion)
    {
        try
        {
            await _servicio.EditarAsync(ObtenerIdUsuarioActual(), id, peticion);

            return Ok(new { mensaje = "Los datos del usuario quedaron guardados." });
        }
        catch (MySqlException ex) when (ex.Number == ErrorReglaDeNegocio)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al editar el usuario.",
                detalle = ex.Message
            });
        }
    }

    // Activar o inactivar. Los usuarios no se borran: la baja es logica.
    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoUsuarioRequest peticion)
    {
        if (id == ObtenerIdUsuarioActual())
        {
            return BadRequest(new { mensaje = "No puede cambiar el estado de su propia cuenta." });
        }

        try
        {
            await _servicio.CambiarEstadoAsync(ObtenerIdUsuarioActual(), id, peticion.IdEstadoUsuario);

            return Ok(new
            {
                mensaje = peticion.IdEstadoUsuario == EstadoActivo
                    ? "La cuenta quedó activa."
                    : "La cuenta quedó inactiva."
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
                mensaje = "Error al cambiar el estado del usuario.",
                detalle = ex.Message
            });
        }
    }

    // Vuelve a mandar el enlace. Sirve para la invitacion que no llego y para
    // el restablecimiento pedido por telefono.
    [HttpPost("{id:int}/invitacion")]
    public async Task<IActionResult> ReenviarInvitacion(int id)
    {
        try
        {
            var resultado = await _servicio.ReenviarEnlaceAsync(id);

            return Ok(Describir(resultado,
                "Se envió el enlace al correo de la persona.",
                "No se pudo enviar el correo. El enlace quedó generado."));
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

    // La respuesta es 200 aunque el correo no salga: la cuenta y el enlace si
    // quedaron. "correoEnviado" es lo que la pantalla usa para decidir el tono
    // del aviso, y en desarrollo va el enlace para poder seguir la prueba.
    private Dictionary<string, object?> Describir(
        ResultadoInvitacion resultado, string siSalio, string siNoSalio)
    {
        var respuesta = new Dictionary<string, object?>
        {
            ["mensaje"] = resultado.CorreoEnviado ? siSalio : siNoSalio,
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

        return respuesta;
    }
}
