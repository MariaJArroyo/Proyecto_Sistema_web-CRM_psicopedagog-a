
namespace MenteActiva.Api.Models;

public class DashboardResponse
{
    // Indicadores generales
    public int EstudiantesActivos { get; set; }
    public int CitasSemana { get; set; }
    public int PagosPendientes { get; set; }
    public int SolicitudesSinAtender { get; set; }

    // Estadísticas de reportes
    public int ReportesGenerados { get; set; }
    public int SesionesRealizadasMes { get; set; }

    // Listas del dashboard
    public List<DashboardCitaResponse> CitasSemanaDetalle { get; set; } = new();

    public List<DashboardAlertaResponse> Alertas { get; set; } = new();

    public List<DashboardActividadResponse> ActividadesRecientes { get; set; } = new();
}

public class DashboardCitaResponse
{
    public int IdCita { get; set; }
    public string NombreEstudiante { get; set; } = string.Empty;
    public string TipoSesion { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class DashboardAlertaResponse
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public class DashboardActividadResponse
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
}
