
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class DashboardController : ControllerBase
{
    private readonly IConfiguration _config;

    public DashboardController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        using var conexion = new MySqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        using var resultado = await conexion.QueryMultipleAsync(
            "SP_ConsultarDashboard",
            commandType: CommandType.StoredProcedure);

        // 1. Indicadores generales
        var dashboard = await resultado
            .ReadFirstOrDefaultAsync<DashboardResponse>();

        if (dashboard == null)
        {
            dashboard = new DashboardResponse();
        }

        // 2. Citas de la semana
        dashboard.CitasSemanaDetalle = (
            await resultado.ReadAsync<DashboardCitaResponse>()
        ).ToList();

        // 3. Alertas importantes
        dashboard.Alertas = (
            await resultado.ReadAsync<DashboardAlertaResponse>()
        ).ToList();

        // 4. Actividad reciente
        dashboard.ActividadesRecientes = (
            await resultado.ReadAsync<DashboardActividadResponse>()
        ).ToList();

        return Ok(dashboard);
    }
}
