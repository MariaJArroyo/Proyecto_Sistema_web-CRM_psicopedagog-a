using Dapper;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Security.Claims;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class EstudiantesController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly RepositorioEstudiante _repositorioEstudiante;

    public EstudiantesController(
        IConfiguration config,
        RepositorioEstudiante repositorioEstudiante)
    {
        _config = config;
        _repositorioEstudiante = repositorioEstudiante;
    }

    #region VerEstudiante
    [HttpGet]
    public IActionResult ConsultarEstudiantes([FromQuery] bool incluirInactivos = false)
    {
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_IncluirInactivos", incluirInactivos);

        var estudiantes = conexion.Query<EstudianteResponse>(
            "SP_ConsultarEstudiantes_CRM",
            parametros,
            commandType: CommandType.StoredProcedure
        ).ToList();

        return Ok(estudiantes);
    }
    #endregion


    #region ver ficha
    [HttpGet("{id:int}")]
    public IActionResult ConsultarFichaEstudiante(int id)
    {
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", id);

        using var resultados = conexion.QueryMultiple(
            "SP_ConsultarFichaEstudiante_CRM",
            parametros,
            commandType: CommandType.StoredProcedure);

        var estudiante =
            resultados.ReadFirstOrDefault<FichaEstudianteResponse>();

        if (estudiante == null)
        {
            return NotFound(new
            {
                mensaje = "No se encontró el estudiante."
            });
        }

        estudiante.Areas =
            resultados.Read<AreaEstudianteResponse>().ToList();

        estudiante.Encargados =
            resultados.Read<EncargadoEstudianteResponse>().ToList();

        return Ok(estudiante);
    }

    #endregion

    #region BuscarEncargados

    // Lookup de persona encargada (nombre, cédula o correo)
    [HttpGet("encargados")]
    public async Task<IActionResult> BuscarEncargados(
        [FromQuery] string? texto,
        CancellationToken cancelacion)
    {
        var encargados = await _repositorioEstudiante.BuscarEncargadosAsync(
            texto,
            cancelacion);

        return Ok(encargados);
    }

    #endregion

    #region CrearEstudiante

    [HttpPost]
    public async Task<IActionResult> CrearEstudiante(
        [FromBody] NuevoEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (request.FechaNacimiento?.Date > DateTime.Today)
        {
            return BadRequest(new { mensaje = "La fecha de nacimiento no puede ser futura." });
        }

        var idEstudiante = await _repositorioEstudiante.CrearConEncargadoAsync(
            ObtenerIdUsuarioActual(),
            request,
            cancelacion);

        return Ok(new
        {
            idEstudiante,
            mensaje = "Estudiante registrado correctamente."
        });
    }

    #endregion

    #region AgregarAreadeDificultad

    [HttpPost("{id:int}/areas")]
    public async Task<IActionResult> AgregarArea(
        int id,
        [FromBody] AgregarAreaEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El estudiante indicado no es válido."
            });
        }

        await _repositorioEstudiante.AgregarAreaAsync(
            id,
            request.NombreArea,
            request.Observacion,
            cancelacion);

        return Ok(new
        {
            mensaje = "Área de dificultad registrada correctamente."
        });
    }

    #endregion

    #region EditarAreadeDificultad

    [HttpPut("{id:int}/areas/{idArea:int}")]
    public async Task<IActionResult> EditarArea(
        int id,
        int idArea,
        [FromBody] AgregarAreaEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0 || idArea <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Los datos indicados no son válidos."
            });
        }

        await _repositorioEstudiante.EditarAreaAsync(
            id,
            idArea,
            request.NombreArea,
            request.Observacion,
            cancelacion);

        return Ok(new
        {
            mensaje = "Área de dificultad actualizada correctamente."
        });
    }

    #endregion

    #region EditarEstudiante

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditarEstudiante(
        int id,
        [FromBody] EditarEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new { mensaje = "El estudiante indicado no es válido." });
        }

        if (request.FechaNacimiento?.Date > DateTime.Today)
        {
            return BadRequest(new { mensaje = "La fecha de nacimiento no puede ser futura." });
        }

        await _repositorioEstudiante.EditarFichaAsync(
            ObtenerIdUsuarioActual(),
            id,
            request,
            cancelacion);

        return Ok(new { mensaje = "Ficha del estudiante actualizada correctamente." });
    }

    [HttpPut("{id:int}/desactivar")]
    public async Task<IActionResult> DesactivarEstudiante(int id, CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new { mensaje = "El estudiante indicado no es válido." });
        }

        await _repositorioEstudiante.DesactivarAsync(ObtenerIdUsuarioActual(), id, cancelacion);

        return Ok(new { mensaje = "Estudiante desactivado correctamente." });
    }

    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    #endregion

    #region Lookups y necesidades de apoyo

    // Rutas literales: no chocan con {id:int}
    [HttpGet("areas/buscar")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> BuscarAreas(
        [FromQuery] string? texto, CancellationToken cancelacion)
        => Ok(await _repositorioEstudiante.BuscarAreasDificultadAsync(texto, cancelacion));

    [HttpGet("necesidades/buscar")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> BuscarNecesidades(
        [FromQuery] string? texto, CancellationToken cancelacion)
        => Ok(await _repositorioEstudiante.BuscarNecesidadesApoyoAsync(texto, cancelacion));

    [HttpGet("{id:int}/necesidades")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> ListarNecesidades(int id, CancellationToken cancelacion)
    {
        if (id <= 0)
            return BadRequest(new { mensaje = "El estudiante indicado no es válido." });

        return Ok(await _repositorioEstudiante.ListarNecesidadesAsync(id, cancelacion));
    }

    [HttpPost("{id:int}/necesidades")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> AgregarNecesidad(
        int id,
        [FromBody] AgregarNecesidadEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0)
            return BadRequest(new { mensaje = "El estudiante indicado no es válido." });

        await _repositorioEstudiante.AgregarNecesidadAsync(
            id, request.NombreNecesidad, request.Observacion, cancelacion);

        return Ok(new { mensaje = "Necesidad de apoyo registrada correctamente." });
    }


    [HttpPut("{id:int}/necesidades/{idNecesidad:int}")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> EditarNecesidad(
        int id,
        int idNecesidad,
        [FromBody] EditarNecesidadEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0 || idNecesidad <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Los datos indicados no son válidos."
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _repositorioEstudiante.EditarNecesidadAsync(
            id,
            idNecesidad,
            request.NombreNecesidad,
            request.Observacion,
            cancelacion);

        return Ok(new
        {
            mensaje = "Necesidad de apoyo actualizada correctamente."
        });
    }


    [HttpDelete("{id:int}/necesidades/{idNecesidad:int}")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> QuitarNecesidad(
        int id, int idNecesidad, CancellationToken cancelacion)
    {
        if (id <= 0 || idNecesidad <= 0)
            return BadRequest(new { mensaje = "Los datos indicados no son válidos." });

        await _repositorioEstudiante.QuitarNecesidadAsync(id, idNecesidad, cancelacion);

        return Ok(new { mensaje = "Necesidad de apoyo quitada correctamente." });
    }

    #endregion

    #region Información académica adicional

    [HttpGet("{id:int}/info-academica")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> ListarInfoAcademica(int id, CancellationToken cancelacion)
    {
        if (id <= 0)
            return BadRequest(new { mensaje = "El estudiante indicado no es válido." });

        return Ok(await _repositorioEstudiante.ListarInfoAcademicaAsync(id, cancelacion));
    }

    [HttpPost("{id:int}/info-academica")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> AgregarInfoAcademica(
        int id, [FromBody] GuardarInfoAcademicaRequest request, CancellationToken cancelacion)
    {
        if (id <= 0)
            return BadRequest(new { mensaje = "El estudiante indicado no es válido." });

        await _repositorioEstudiante.AgregarInfoAcademicaAsync(id, request, cancelacion);

        return Ok(new { mensaje = "Información académica registrada correctamente." });
    }

    [HttpPut("{id:int}/info-academica/{idInfo:int}")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> EditarInfoAcademica(
        int id, int idInfo, [FromBody] GuardarInfoAcademicaRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0 || idInfo <= 0)
            return BadRequest(new { mensaje = "Los datos indicados no son válidos." });

        await _repositorioEstudiante.EditarInfoAcademicaAsync(id, idInfo, request, cancelacion);

        return Ok(new { mensaje = "Información académica actualizada correctamente." });
    }

    [HttpDelete("{id:int}/info-academica/{idInfo:int}")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> QuitarInfoAcademica(
        int id, int idInfo, CancellationToken cancelacion)
    {
        if (id <= 0 || idInfo <= 0)
            return BadRequest(new { mensaje = "Los datos indicados no son válidos." });

        await _repositorioEstudiante.QuitarInfoAcademicaAsync(id, idInfo, cancelacion);

        return Ok(new { mensaje = "Información académica quitada correctamente." });
    }


    [HttpPut("{id:int}/resumen")]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> ActualizarResumen(
        int id,
        [FromBody] ActualizarResumenEstudianteRequest request,
        CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El estudiante indicado no es válido."
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _repositorioEstudiante.ActualizarResumenAsync(
            id,
            request.Resumen,
            cancelacion);

        return Ok(new
        {
            mensaje = string.IsNullOrWhiteSpace(request.Resumen)
                ? "Resumen eliminado correctamente."
                : "Resumen actualizado correctamente."
        });
    }
    #endregion
}