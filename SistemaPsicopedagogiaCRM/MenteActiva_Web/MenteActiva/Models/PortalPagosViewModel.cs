namespace MenteActiva.Models;

public class PortalPagosViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public int IdEstudiante { get; set; }

    public string Filtro { get; set; } = "todos";

    public List<PortalPagoViewModel> Pagos { get; set; } = [];

    // Pagos que la familia reporto para este estudiante (SINPE o transferencia)
    public List<PortalReportePagoViewModel> Reportes { get; set; } = [];
}