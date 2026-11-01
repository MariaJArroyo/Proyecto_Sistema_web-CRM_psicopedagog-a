namespace MenteActiva.Models;

public class PortalProgresoViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public int IdEstudiante { get; set; }

    public string FiltroReportes { get; set; } = "todos";

    public List<PortalProgresoSesionViewModel> Sesiones { get; set; } = [];

    public List<PortalReporteListaViewModel> Reportes { get; set; } = [];
}