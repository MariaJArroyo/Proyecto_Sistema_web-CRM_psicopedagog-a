namespace MenteActiva.Models;

public class PortalInicioViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public PortalResumenViewModel Resumen { get; set; } = new();
}
