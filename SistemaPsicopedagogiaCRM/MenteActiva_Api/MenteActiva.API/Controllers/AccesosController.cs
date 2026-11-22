using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenteActiva.Api.Controllers;

// Bitacora de inicios de sesion (solo lectura: los registros los escribe el
// login). Dos consultas:
//   - api/usuarios/{id}/accesos: el historial de una cuenta. Administrador ve
//     cualquiera; Psicopedagoga solo las de encargados (usuarios externos),
//     que es lo que ella administra.
//   - api/accesos: la bitacora completa con filtros, solo Administrador.
[ApiController]
public sealed class AccesosController : ControllerBase
{
    private const int LimiteHistorial = 50;
    private const int LimiteBitacora = 500;

    private static readonly string[] Tipos = { "Internos", "Externos", "Desconocidos" };

    private readonly IRepositorioAcceso _repositorio;
    private readonly IRepositorioUsuario _usuarios;

    public AccesosController(IRepositorioAcceso repositorio, IRepositorioUsuario usuarios)
    {
        _repositorio = repositorio;
        _usuarios = usuarios;
    }

    [HttpGet("api/usuarios/{idUsuario:int}/accesos")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<ActionResult<IReadOnlyList<AccesoUsuarioResponse>>> HistorialUsuario(
        int idUsuario, CancellationToken cancelacion)
    {
        if (!User.IsInRole("Administrador"))
        {
            var usuario = await _usuarios.ObtenerPorIdAsync(idUsuario);

            if (usuario is null)
            {
                return NotFound(new { mensaje = "El usuario indicado no existe." });
            }

            if (usuario.IdEncargado is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "Solo el administrador puede ver los accesos del personal." });
            }
        }

        return Ok(await _repositorio.ListarPorUsuarioAsync(idUsuario, LimiteHistorial, cancelacion));
    }

    [HttpGet("api/accesos")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IReadOnlyList<AccesoBitacoraResponse>>> Bitacora(
        [FromQuery] FiltroAccesosRequest filtro, CancellationToken cancelacion)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Tipo)
            && !Tipos.Contains(filtro.Tipo.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ReglaNegocioException("El tipo de usuario indicado no es válido.");
        }

        if (!string.IsNullOrWhiteSpace(filtro.Resultado)
            && filtro.Resultado != "Fallido"
            && !ResultadosAcceso.Todos.Contains(filtro.Resultado))
        {
            throw new ReglaNegocioException("El resultado indicado no es válido.");
        }

        if (filtro.Desde is not null && filtro.Hasta is not null && filtro.Desde > filtro.Hasta)
        {
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");
        }

        filtro.Tipo = Tipos.FirstOrDefault(t => string.Equals(t, filtro.Tipo?.Trim(), StringComparison.OrdinalIgnoreCase));
        filtro.Texto = string.IsNullOrWhiteSpace(filtro.Texto) ? null : filtro.Texto.Trim();

        return Ok(await _repositorio.ListarAsync(filtro, LimiteBitacora, cancelacion));
    }
}
