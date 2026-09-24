using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MenteActiva.Api.Models;
using System.Data;

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
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var clientes = conexion.Query<ClienteResponse>(
            "SP_ConsultarClientes_CRM",
            commandType: CommandType.StoredProcedure
        ).ToList();

        return Ok(clientes);
    }

    #endregion

    #region Registrar clientes

    [HttpPost]
    public IActionResult RegistrarCliente([FromBody] ClienteRequest request)
    {
        // Validar modelo
        if (!ModelState.IsValid || request.IdServicioInteres <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe seleccionar un servicio de interés."
            });
        }

        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();

        parametros.Add(
            "p_NombreEncargado",
            request.NombreEncargado);

        parametros.Add(
            "p_ApellidoEncargado",
            request.ApellidoEncargado);

        parametros.Add(
            "p_Telefono",
            request.Telefono);

        parametros.Add(
            "p_Correo",
            request.Correo);

        parametros.Add(
            "p_IdServicioInteres",
            request.IdServicioInteres);

        parametros.Add(
            "p_NombreEstudiante",
            request.NombreEstudiante);

        parametros.Add(
            "p_ApellidoEstudiante",
            request.ApellidoEstudiante);

        parametros.Add(
            "p_Observaciones",
            request.Observaciones);

        try
        {
            conexion.Execute(
                "SP_RegistrarCliente_CRM",
                parametros,
                commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                mensaje = "Cliente registrado correctamente."
            });
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

    #endregion

    #region Desactivar Cliente

    [HttpPut("{id}")]
    public IActionResult DesactivarCliente(int id)
    {
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        var parametros = new DynamicParameters();
        parametros.Add("p_IdEncargado", id);

        try
        {
            conexion.Execute(
                "SP_DesactivarCliente_CRM",
                parametros,
                commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                mensaje = "Cliente desactivado correctamente."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al desactivar el cliente.",
                detalle = ex.Message
            });
        }
    }

    #endregion




}