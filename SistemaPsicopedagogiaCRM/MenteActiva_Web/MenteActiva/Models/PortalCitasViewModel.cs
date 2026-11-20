namespace MenteActiva.Models;

public class PortalCitasViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public int IdEstudiante { get; set; }

    public string Filtro { get; set; } = "todas";

    public List<PortalCitaViewModel> Citas { get; set; } = [];
}