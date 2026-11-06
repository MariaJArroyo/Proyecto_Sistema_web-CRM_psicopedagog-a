using Dapper;
using MenteActiva.Api.Models;
using MenteActiva.API.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;

namespace MenteActiva.Api.Controllers;

// Lo unico del CRM que se puede usar sin sesion: el formulario del sitio publico
[ApiController]
[Route("api/contacto")]
// Anonimo a proposito y es el unico del CRM que lo es: lo llama el formulario
// del sitio publico, donde no hay sesion. No es un olvido.
[AllowAnonymous]
public class ContactoController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IServicioCorreo _correo;
    private readonly IServicioConfiguracion _configuracion;
    private readonly ILogger<ContactoController> _log;

    public ContactoController(
        IConfiguration config,
        IServicioCorreo correo,
        IServicioConfiguracion configuracion,
        ILogger<ContactoController> log)
    {
        _config = config;
        _correo = correo;
        _configuracion = configuracion;
        _log = log;
    }

    // Servicios activos para el <select> del formulario publico
    [HttpGet("servicios")]
    public IActionResult ListarServicios()
    {
        using var conexion = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        var servicios = conexion.Query<ServicioResponse>(
            "SP_ListarServiciosActivos_CRM",
            commandType: CommandType.StoredProcedure).ToList();

        return Ok(servicios);
    }

    [HttpPost]
    public async Task<IActionResult> EnviarSolicitud([FromBody] SolicitudContactoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { mensaje = "Revise los datos del formulario." });
        }

        using var conexion = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_Nombre", request.Nombre.Trim());
        parametros.Add("p_Apellido", request.Apellido.Trim());
        parametros.Add("p_Telefono", request.Telefono.Trim());
        parametros.Add("p_Correo", request.Correo.Trim());
        parametros.Add("p_IdServicioInteres", request.IdServicioInteres);
        parametros.Add("p_Mensaje", request.Mensaje.Trim());
        parametros.Add("p_DireccionIp", request.DireccionIp);

        SolicitudRegistrada registrada;

        try
        {
            registrada = await conexion.QuerySingleAsync<SolicitudRegistrada>(
                "SP_RegistrarSolicitudContacto_CRM",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (MySqlException ex) when (ex.SqlState == "45000")
        {
            // Limite por IP u otra regla de la SP
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al registrar una solicitud de contacto.");
            return StatusCode(500, new { mensaje = "No se pudo enviar el mensaje. Intente de nuevo más tarde." });
        }

        await EnviarCorreosAsync(registrada, request);

        return Ok(new { mensaje = "Mensaje enviado correctamente." });
    }

    // Los dos correos son un extra: si fallan, la solicitud ya quedo guardada y se
    // ve en el panel. Por eso cada uno va en su propio try y ninguno cambia la
    // respuesta que recibe el visitante.
    //
    // Ninguno recibe el token de cancelacion de la peticion a proposito: ya se
    // guardo, y los correos deben salir aunque el visitante cierre la pagina sin
    // esperar la respuesta.
    private async Task EnviarCorreosAsync(SolicitudRegistrada solicitud, SolicitudContactoRequest peticion)
    {
        var consultorio = await LeerDatosConsultorioAsync(solicitud.IdSolicitud);

        await AvisarConsultorioAsync(solicitud, peticion, consultorio);
        await ConfirmarVisitanteAsync(solicitud, peticion, consultorio);
    }

    // Si la configuracion no se puede leer, los correos salen igual con el nombre
    // por defecto y sin las formas de contacto, que es mejor que no salir
    private async Task<DatosConsultorioResponse> LeerDatosConsultorioAsync(int idSolicitud)
    {
        try
        {
            return await _configuracion.ObtenerDatosConsultorioAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "No se pudieron leer los datos del consultorio para los correos de la solicitud {IdSolicitud}.",
                idSolicitud);

            return new DatosConsultorioResponse();
        }
    }

    private async Task AvisarConsultorioAsync(
        SolicitudRegistrada solicitud,
        SolicitudContactoRequest peticion,
        DatosConsultorioResponse consultorio)
    {
        var destinatario = _config["Correo:DestinatarioContacto"];

        if (string.IsNullOrWhiteSpace(destinatario))
        {
            destinatario = _config["Correo:Remitente"];
        }

        if (string.IsNullOrWhiteSpace(destinatario))
        {
            _log.LogWarning(
                "La solicitud {IdSolicitud} no se aviso por correo: falta Correo:DestinatarioContacto.",
                solicitud.IdSolicitud);

            return;
        }

        var urlPanel = $"{(_config["Correo:UrlBaseWeb"] ?? string.Empty).TrimEnd('/')}/Admin/Solicitudes";

        try
        {
            await _correo.EnviarAsync(
                destinatario,
                MensajesSolicitudContacto.AsuntoAviso,
                MensajesSolicitudContacto.Aviso(
                    solicitud.IdSolicitud,
                    peticion,
                    solicitud.Servicio,
                    solicitud.FechaRegistro,
                    consultorio,
                    urlPanel));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudo enviar el aviso de la solicitud {IdSolicitud}.", solicitud.IdSolicitud);
        }
    }

    private async Task ConfirmarVisitanteAsync(
        SolicitudRegistrada solicitud,
        SolicitudContactoRequest peticion,
        DatosConsultorioResponse consultorio)
    {
        try
        {
            await _correo.EnviarAsync(
                peticion.Correo.Trim(),
                MensajesSolicitudContacto.AsuntoConfirmacion(consultorio),
                MensajesSolicitudContacto.Confirmacion(peticion, solicitud.FechaRegistro, consultorio));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "No se pudo enviar la confirmacion de la solicitud {IdSolicitud}.", solicitud.IdSolicitud);
        }
    }

    // Lo que devuelve SP_RegistrarSolicitudContacto_CRM. El servicio lo resuelve
    // la base porque el procedimiento descarta uno inactivo, y la fecha es la que
    // quedo guardada, que es la que se cita en el correo al visitante.
    private sealed class SolicitudRegistrada
    {
        public int IdSolicitud { get; set; }

        public string? Servicio { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}