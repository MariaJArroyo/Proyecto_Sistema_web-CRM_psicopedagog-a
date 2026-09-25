namespace MenteActiva.Models;

public class ClienteRegistroViewModel : ClienteRequestViewModel
{
    public List<EstudianteClienteViewModel> Estudiantes { get; set; } = new();
}