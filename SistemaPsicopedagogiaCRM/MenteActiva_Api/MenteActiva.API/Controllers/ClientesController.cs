using Dapper;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.API.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Security.Claims;
using System.Text.Json;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class ClientesController : ControllerBase
{
    private readonly IConfiguration _config;

    // Las dos acciones de estado del encargado pasan por servicio y repositorio
    // (RG-117). Las otras ocho siguen abriendo su propia conexion aqui abajo:
    // bajarlas a capas es un trabajo aparte y esta anotado como deuda.
    private readonly IServicioEncargado _encargados;

    // Solo para la casilla de invitacion del alta: el alta crea al encargado y
    // esto le habilita el portal, en ese orden y nunca al reves.
    private readonly IServicioUsuarios _usuarios;

    private readonly ILogger<ClientesController> _registro;

    public ClientesController(
        IConfiguration config,
        IServicioEncargado encargados,
        IServicioUsuarios usuarios,
        ILogger<ClientesController> registro)
    {
        _config = config;
        _encargados = encargados;
        _usuarios = usuarios;
        _registro = registro;
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
    public async Task<IActionResult> RegistrarCliente([FromBody] ClienteRegistroRequest request)
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
        parametros.Add("p_IdSolicitud", request.IdSolicitud);

        int idEncargado;

        using (var conexion = CrearConexion())
        {
            try
            {
                idEncargado = await conexion.QuerySingleAsync<int>(
                    "SP_RegistrarCliente_CRM",
                    parametros,
                    commandType: CommandType.StoredProcedure);
            }
            catch (MySqlException ex) when (ex.SqlState == "45000")
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al registrar el cliente.",
                    detalle = ex.Message
                });
            }
        }

        // De aqui para abajo el cliente YA quedo guardado y la solicitud, si
        // venia una, ya quedo convertida. Nada de lo que sigue puede devolver un
        // error: la pantalla lo leeria como que no se guardo nada. Lo que falle
        // se cuenta en el mensaje, con correoEnviado en false (RG-123).
        // Sin correo la pantalla deja la casilla apagada, asi que la peticion
        // llega en false igual que si la hubieran desmarcado a mano. El aviso
        // tiene que decir por que no se invito, o parece que se olvido.
        if (!request.EnviarInvitacion)
        {
            return Ok(new
            {
                mensaje = string.IsNullOrWhiteSpace(request.Correo)
                    ? "Cliente registrado. Sin correo no se puede invitar al portal."
                    : "Cliente registrado correctamente.",
                idEncargado
            });
        }

        if (string.IsNullOrWhiteSpace(request.Correo))
        {
            return Ok(new
            {
                mensaje = "Cliente registrado. No se invitó al portal porque no tiene correo.",
                idEncargado,
                correoEnviado = false
            });
        }

        return await InvitarAlPortalAsync(idEncargado);
    }

    // Habilita el portal al encargado recien creado. Devuelve siempre 200: el
    // alta ya esta hecha y lo unico que cambia es el aviso.
    private async Task<IActionResult> InvitarAlPortalAsync(int idEncargado)
    {
        try
        {
            var resultado = await _usuarios.InvitarEncargadoAsync(ObtenerIdUsuarioActual(), idEncargado);

            return Ok(new
            {
                mensaje = resultado.CorreoEnviado
                    ? "Cliente registrado. Se envió la invitación al portal."
                    : "Cliente registrado y cuenta del portal creada, pero el correo no salió. "
                      + "Use \"Reenviar invitación\" en Acceso al portal.",
                idEncargado,
                correoEnviado = resultado.CorreoEnviado
            });
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            // Las cuatro negativas de SP_UsuarioExterno_Crear: correo de otro
            // usuario, ya tiene portal, sin correo, o encargado inactivo
            return Ok(new
            {
                mensaje = $"Cliente registrado, pero no se pudo habilitar el portal: {ex.Message}",
                idEncargado,
                correoEnviado = false
            });
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "El encargado {IdEncargado} quedo creado pero fallo la invitacion al portal.",
                idEncargado);

            return Ok(new
            {
                mensaje = "Cliente registrado, pero no se pudo habilitar el portal. "
                        + "Invítelo desde Acceso al portal.",
                idEncargado,
                correoEnviado = false
            });
        }
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

    #region Desactivar y reactivar cliente

    // Las reglas ("no existe", "ya esta inactivo") salen del procedimiento como
    // SIGNAL 45000 y ManejadorExcepciones las responde como 400 con su mensaje,
    // asi que aqui no hay try/catch.
    [HttpPut("{id:int}/desactivar")]
    public async Task<IActionResult> DesactivarCliente(int id, CancellationToken cancelacion)
    {
        await _encargados.DesactivarAsync(User.ObtenerIdUsuario(), id, cancelacion);

        return Ok(new { mensaje = "Cliente desactivado correctamente." });
    }

    [HttpPut("{id:int}/reactivar")]
    public async Task<IActionResult> ReactivarCliente(int id, CancellationToken cancelacion)
    {
        await _encargados.ReactivarAsync(User.ObtenerIdUsuario(), id, cancelacion);

        return Ok(new { mensaje = "Cliente reactivado correctamente." });
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