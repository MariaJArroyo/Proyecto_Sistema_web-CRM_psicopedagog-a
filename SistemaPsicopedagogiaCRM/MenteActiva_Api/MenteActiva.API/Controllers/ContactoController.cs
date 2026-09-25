using Dapper;
using MenteActiva.Api.Models;
using MenteActiva.API.Models;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Net;

namespace MenteActiva.Api.Controllers;

// Lo unico del CRM que se puede usar sin sesion: el formulario del sitio publico
[ApiController]
[Route("api/contacto")]
[AllowAnonymous]
public class ContactoController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IServicioCorreo _correo;
    private readonly ILogger<ContactoController> _log;

    public ContactoController(IConfiguration config, IServicioCorreo correo, ILogger<ContactoController> log)
    {
        _config = config;
        _correo = correo;
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

        int idSolicitud;

        try
        {
            idSolicitud = await conexion.ExecuteScalarAsync<int>(
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

        await NotificarConsultorioAsync(idSolicitud, request);

        return Ok(new { mensaje = "Mensaje enviado correctamente." });
    }

    // El aviso es un extra: si falla, la solicitud ya esta guardada y se ve en el panel
    private async Task NotificarConsultorioAsync(int idSolicitud, SolicitudContactoRequest s)
    {
        var destinatario = _config["Correo:DestinatarioContacto"];
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            destinatario = _config["Correo:Remitente"];
        }

        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return;
        }

        // Todo lo que escribio el visitante va codificado para que no inyecte HTML
        string C(string? texto) => WebUtility.HtmlEncode(texto ?? string.Empty);

        var urlPanel = $"{_config["Correo:UrlBaseWeb"]}/Admin/Solicitudes";

        var cuerpo = $"""
            <h2>Nueva solicitud de contacto</h2>
            <p><strong>Nombre:</strong> {C(s.Nombre)} {C(s.Apellido)}</p>
            <p><strong>Teléfono:</strong> {C(s.Telefono)}</p>
            <p><strong>Correo:</strong> {C(s.Correo)}</p>
            <p><strong>Mensaje:</strong><br>{C(s.Mensaje).Replace("\n", "<br>")}</p>
            <p><a href="{urlPanel}">Ver solicitudes en el panel</a> (solicitud #{idSolicitud})</p>
            """;

        try
        {
            await _correo.EnviarAsync(destinatario, "Nueva solicitud de contacto - Mente Activa", cuerpo);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudo enviar el aviso de la solicitud {IdSolicitud}.", idSolicitud);
        }
    }
}