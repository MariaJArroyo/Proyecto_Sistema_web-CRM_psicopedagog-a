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
                ["mensaje"] = "Se envió la invitación al portal.",
                ["idUsuario"] = resultado.IdUsuario
            };

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

    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
