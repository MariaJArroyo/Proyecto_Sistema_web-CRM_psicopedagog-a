
namespace MenteActiva.Models;

public class DashboardViewModel
{
    public int EstudiantesActivos { get; set; }
    public int CitasSemana { get; set; }
    public int PagosPendientes { get; set; }
    public int SolicitudesSinAtender { get; set; }

    public int ReportesGenerados { get; set; }
    public int SesionesRealizadasMes { get; set; }

    public List<DashboardCitaViewModel> CitasSemanaDetalle { get; set; } = new();

    public List<DashboardAlertaViewModel> Alertas { get; set; } = new();

    public List<DashboardActividadViewModel> ActividadesRecientes { get; set; } = new();
}

public class DashboardCitaViewModel
{
    public int IdCita { get; set; }
    public string NombreEstudiante { get; set; } = string.Empty;
    public string TipoSesion { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class DashboardAlertaViewModel
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public class DashboardActividadViewModel
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
}
