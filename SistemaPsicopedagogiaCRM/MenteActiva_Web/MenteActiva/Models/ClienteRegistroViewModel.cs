namespace MenteActiva.Models;

public class ClienteRegistroViewModel : ClienteRequestViewModel
{
    public int? IdSolicitud { get; set; }

    // Casilla del modal. Sin marcar, o deshabilitada por falta de correo, llega
    // en false porque el navegador no manda las casillas apagadas.
    public bool EnviarInvitacion { get; set; }

    public List<EstudianteClienteViewModel> Estudiantes { get; set; } = new();
}