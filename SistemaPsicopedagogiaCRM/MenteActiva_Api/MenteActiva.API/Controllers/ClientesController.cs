using Dapper;
using MenteActiva.Api.Models;
using MenteActiva.API.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Security.Claims;
using System.Text.Json;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IConfiguration _config;

    public ClientesController(IConfiguration config)
    {
        _config = config;
    }

    #region Consultar clientes

    [HttpGet]
    public IActionResult ConsultarClientes()
    {
        using var conexion = CrearConexion();

        var clientes = conexion.Query<ClienteResponse>(
            "SP_ConsultarClientes_CRM",
            commandType: CommandType.StoredProcedure
        ).ToList();

        return Ok(clientes);
    }

    #endregion

    #region Obtener cliente (encargado + estudiantes)

    [HttpGet("{id:int}")]
    public IActionResult ObtenerCliente(int id)
    {
        using var conexion = CrearConexion();

        var parametros = new DynamicParameters();
        parametros.Add("p_IdEncargado", id);

        // La SP devuelve dos resultados: el encargado y sus estudiantes
        using var resultados = conexion.QueryMultiple(
            "SP_ObtenerCliente_CRM",
            parametros,
            commandType: CommandType.StoredProcedure);

        var cliente = resultados.ReadFirstOrDefault<ClienteDetalleResponse>();

        if (cliente is null)
        {
            return NotFound(new { mensaje = "El cliente no existe." });
        }

        cliente.Estudiantes = resultados.Read<EstudianteClienteResponse>().ToList();

        return Ok(cliente);
    }

    #endregion

    #region Registrar cliente

    [HttpPost]
    public IActionResult RegistrarCliente([FromBody] ClienteRegistroRequest request)
    {
        if (!ModelState.IsValid || request.IdServicioInteres <= 0)
        {
            return BadRequest(new { mensaje = "Revise los datos del cliente." });
        }

        if (request.Estudiantes.Count == 0)
        {
            return BadRequest(new { mensaje = "Debe agregar al menos un estudiante." });
        }

        var errorFecha = ValidarFechas(request.Estudiantes);
        if (errorFecha is not null)
        {
            return BadRequest(new { mensaje = errorFecha });
        }

        // La fecha va como "yyyy-MM-dd" porque JSON_TABLE la lee como DATE
        var estudiantesJson = JsonSerializer.Serialize(
            request.Estudiantes.Select(e => new
            {
                Nombre = e.Nombre.Trim(),
                Apellido = e.Apellido.Trim(),
                e.IdParentesco,
                FechaNacimiento = e.FechaNacimiento?.ToString("yyyy-MM-dd"),
                e.IdNivelEducativo
            }));

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", ObtenerIdUsuarioActual());
        parametros.Add("p_NombreEncargado", request.NombreEncargado.Trim());
        parametros.Add("p_ApellidoEncargado", request.ApellidoEncargado.Trim());
        parametros.Add("p_Telefono", request.Telefono.Trim());
        parametros.Add("p_Correo", request.Correo);
        parametros.Add("p_IdServicioInteres", request.IdServicioInteres);
        parametros.Add("p_Observaciones", request.Observaciones);
        parametros.Add("p_EstudiantesJson", estudiantesJson);

        return EjecutarSp(
            "SP_RegistrarCliente_CRM",
            parametros,
            "Cliente registrado correctamente.",
            "Error al registrar el cliente.");
    }

    #endregion

    #region Editar cliente

    [HttpPut("{id:int}")]
    public IActionResult EditarCliente(int id, [FromBody] ClienteEditarRequest request)
    {
        if (!ModelState.IsValid || request.IdServicioInteres <= 0)
        {
            return BadRequest(new { mensaje = "Revise los datos del cliente." });
        }

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", ObtenerIdUsuarioActual());
        parametros.Add("p_IdEncargado", id);
        parametros.Add("p_NombreEncargado", request.NombreEncargado.Trim());
        parametros.Add("p_ApellidoEncargado", request.ApellidoEncargado.Trim());
        parametros.Add("p_Telefono", request.Telefono.Trim());
        parametros.Add("p_Correo", request.Correo);
        parametros.Add("p_IdServicioInteres", request.IdServicioInteres);
        parametros.Add("p_IdEstadoCliente", request.IdEstadoCliente);
        parametros.Add("p_Observaciones", request.Observaciones);

        return EjecutarSp(
            "SP_EditarCliente_CRM",
            parametros,
            "Cliente actualizado correctamente.",
            "Error al editar el cliente.");
    }

    #endregion

    #region Desactivar cliente

    [HttpPut("{id:int}/desactivar")]
    public IActionResult DesactivarCliente(int id)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdEncargado", id);

        return EjecutarSp(
            "SP_DesactivarCliente_CRM",
            parametros,
            "Cliente desactivado correctamente.",
            "Error al desactivar el cliente.");
    }

    #endregion

    #region Estudiantes del cliente

    [HttpPost("{id:int}/estudiantes")]
    public IActionResult AgregarEstudiante(int id, [FromBody] EstudianteClienteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { mensaje = "Revise los datos del estudiante." });
        }

        var errorFecha = ValidarFechas(new[] { request });
        if (errorFecha is not null)
        {
            return BadRequest(new { mensaje = errorFecha });
        }

        // Se reutiliza SP_Estudiante_Crear; lo que este formulario no maneja va en NULL
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", ObtenerIdUsuarioActual());
        parametros.Add("p_Nombre", request.Nombre.Trim());
        parametros.Add("p_PrimerApellido", request.Apellido.Trim());
        parametros.Add("p_SegundoApellido", null);
        parametros.Add("p_FechaNacimiento", request.FechaNacimiento?.Date);
        parametros.Add("p_IdNivelEducativo", request.IdNivelEducativo);
        parametros.Add("p_IdInstitucion", null);
        parametros.Add("p_Observaciones", null);
        parametros.Add("p_NecesidadesApoyo", null);
        parametros.Add("p_IdEncargado", id);
        parametros.Add("p_IdParentesco", request.IdParentesco);
        parametros.Add("p_AreasJson", null);

        return EjecutarSp(
            "SP_Estudiante_Crear",
            parametros,
            "Estudiante agregado correctamente.",
            "Error al agregar el estudiante.");
    }

    [HttpPut("{id:int}/estudiantes/{idEstudiante:int}")]
    public IActionResult EditarEstudiante(int id, int idEstudiante, [FromBody] EstudianteClienteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { mensaje = "Revise los datos del estudiante." });
        }

        var errorFecha = ValidarFechas(new[] { request });
        if (errorFecha is not null)
        {
            return BadRequest(new { mensaje = errorFecha });
        }

        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", ObtenerIdUsuarioActual());
        parametros.Add("p_IdEncargado", id);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Nombre", request.Nombre.Trim());
        parametros.Add("p_Apellido", request.Apellido.Trim());
        parametros.Add("p_FechaNacimiento", request.FechaNacimiento?.Date);
        parametros.Add("p_IdNivelEducativo", request.IdNivelEducativo);
        parametros.Add("p_IdParentesco", request.IdParentesco);

        return EjecutarSp(
            "SP_EditarEstudianteCliente_CRM",
            parametros,
            "Estudiante actualizado correctamente.",
            "Error al editar el estudiante.");
    }

    [HttpPut("{id:int}/estudiantes/{idEstudiante:int}/estado")]
    public IActionResult CambiarEstadoEstudiante(int id, int idEstudiante, [FromBody] CambiarEstadoEstudianteRequest request)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", ObtenerIdUsuarioActual());
        parametros.Add("p_IdEncargado", id);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Activo", request.Activo);

        return EjecutarSp(
            "SP_CambiarEstadoEstudiante_CRM",
            parametros,
            request.Activo ? "Estudiante activado correctamente." : "Estudiante desactivado correctamente.",
            "Error al cambiar el estado del estudiante.");
    }

    #endregion

    #region Catalogos

    [HttpGet("estados")]
    public IActionResult ListarEstados()
    {
        using var conexion = CrearConexion();

        return Ok(conexion.Query<EstadoClienteResponse>(
            "SP_ListarEstadosCliente_CRM",
            commandType: CommandType.StoredProcedure).ToList());
    }

    [HttpGet("parentescos")]
    public IActionResult ListarParentescos()
    {
        using var conexion = CrearConexion();

        return Ok(conexion.Query<ParentescoResponse>(
            "SP_ListarParentescos_CRM",
            commandType: CommandType.StoredProcedure).ToList());
    }

    [HttpGet("niveles")]
    public IActionResult ListarNivelesEducativos()
    {
        using var conexion = CrearConexion();

        return Ok(conexion.Query<NivelEducativoResponse>(
            "SP_ListarNivelesEducativos_CRM",
            commandType: CommandType.StoredProcedure).ToList());
    }

    #endregion

    #region Auxiliares

    private MySqlConnection CrearConexion()
        => new(_config.GetConnectionString("DefaultConnection"));

    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Ejecuta una SP y traduce el resultado: los SIGNAL '45000' son errores de
    // negocio (400 con su mensaje); cualquier otra cosa es un 500.
    private IActionResult EjecutarSp(
        string nombreSp,
        DynamicParameters parametros,
        string mensajeOk,
        string mensajeError)
    {
        using var conexion = CrearConexion();

        try
        {
            conexion.Execute(nombreSp, parametros, commandType: CommandType.StoredProcedure);
            return Ok(new { mensaje = mensajeOk });
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = mensajeError, detalle = ex.Message });
        }
    }

    private static string? ValidarFechas(IEnumerable<EstudianteClienteRequest> estudiantes)
    {
        if (estudiantes.Any(e => e.FechaNacimiento?.Date > DateTime.Today))
        {
            return "La fecha de nacimiento no puede ser futura.";
        }

        return null;
    }

    #endregion
}