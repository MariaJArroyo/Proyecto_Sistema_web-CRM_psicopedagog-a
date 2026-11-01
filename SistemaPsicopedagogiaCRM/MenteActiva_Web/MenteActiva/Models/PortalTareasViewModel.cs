namespace MenteActiva.Models;

public class PortalTareasViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public int IdEstudiante { get; set; }

    public string Filtro { get; set; } = "pendientes";

    public List<PortalTareaViewModel> Tareas { get; set; } = [];
}