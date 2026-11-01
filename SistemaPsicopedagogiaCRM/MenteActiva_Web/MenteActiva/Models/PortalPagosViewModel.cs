namespace MenteActiva.Models;

public class PortalPagosViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public int IdEstudiante { get; set; }

    public string Filtro { get; set; } = "pendientes";

    public List<PortalPagoViewModel> Pagos { get; set; } = [];
}