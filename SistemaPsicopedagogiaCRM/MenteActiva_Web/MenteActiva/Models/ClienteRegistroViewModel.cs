namespace MenteActiva.Models;

public class ClienteRegistroViewModel : ClienteRequestViewModel
{
    public int? IdSolicitud { get; set; }
    public List<EstudianteClienteViewModel> Estudiantes { get; set; } = new();
}