using Microsoft.AspNetCore.Mvc;
using SistemaPsicopedagogia.Api.Data;

namespace SistemaPsicopedagogia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrmController : ControllerBase
{
    [HttpGet("dashboard")]
    public IActionResult Dashboard() => Ok(CrmData.Dashboard);

    [HttpGet("clientes")]
    public IActionResult Clientes() => Ok(CrmData.Clientes);

    [HttpGet("estudiantes")]
    public IActionResult Estudiantes() => Ok(CrmData.Estudiantes);

    [HttpGet("agenda")]
    public IActionResult Agenda() => Ok(CrmData.Agenda);

    [HttpGet("pagos")]
    public IActionResult Pagos() => Ok(CrmData.Pagos);

    [HttpGet("planes")]
    public IActionResult Planes() => Ok(CrmData.Planes);

    [HttpGet("sesiones")]
    public IActionResult Sesiones() => Ok(CrmData.Sesiones);

    [HttpGet("materiales")]
    public IActionResult Materiales() => Ok(CrmData.Materiales);

    [HttpGet("usuarios")]
    public IActionResult Usuarios() => Ok(CrmData.Usuarios);

    [HttpGet("usuarios-externos")]
    public IActionResult UsuariosExternos() => Ok(CrmData.UsuariosExternos);

    [HttpGet("comunicacion")]
    public IActionResult Comunicacion() => Ok(CrmData.Comunicacion);

    [HttpGet("reportes")]
    public IActionResult Reportes() => Ok(CrmData.Reportes);

    [HttpGet("portal")]
    public IActionResult Portal() => Ok(CrmData.Portal);

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { estado = "API activa", baseDeDatos = false, datos = "quemados del CRM migrado" });
}
