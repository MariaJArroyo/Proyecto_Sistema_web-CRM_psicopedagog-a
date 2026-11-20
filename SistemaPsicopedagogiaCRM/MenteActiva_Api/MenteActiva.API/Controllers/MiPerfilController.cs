
using System.Security.Claims;
using MenteActiva.Api.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/mi-perfil")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class MiPerfilController : ControllerBase
{
    private const int ErrorReglaDeNegocio = 1644;

    private readonly IServicioUsuarios _servicio;

    public MiPerfilController(IServicioUsuarios servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerMiPerfil()
    {
        var idUsuario = ObtenerIdUsuarioActual();

        var perfil = await _servicio.ObtenerPerfilAsync(idUsuario);

        if (perfil is null)
        {
            return NotFound(new
            {
                mensaje = "No se encontró el perfil del usuario."
            });
        }

        return Ok(perfil);
    }

    [HttpPut]
    public async Task<IActionResult> ActualizarMiPerfil(
        [FromBody] EditarMiPerfilRequest peticion)
    {
        try
        {
            var idUsuario = ObtenerIdUsuarioActual();

            await _servicio.ActualizarPerfilAsync(
                idUsuario,
                peticion.NombreCompleto,
                peticion.Correo,
                peticion.Telefono);

            return Ok(new
            {
                mensaje = "El perfil se actualizó correctamente."
            });
        }
        catch (MySqlException ex) when (ex.Number == ErrorReglaDeNegocio)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    private int ObtenerIdUsuarioActual()
    {
        var idUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(idUsuario, out var resultado))
        {
            throw new UnauthorizedAccessException(
                "No se pudo identificar al usuario autenticado.");
        }

        return resultado;
    }
}
