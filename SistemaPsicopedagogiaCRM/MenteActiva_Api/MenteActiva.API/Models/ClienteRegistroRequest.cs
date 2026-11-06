using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class ClienteRegistroRequest : ClienteRequest
{
    public int? IdSolicitud { get; set; }

    // Casilla "Enviar invitacion al portal al guardar" del modal de alta. Si el
    // encargado no tiene correo la pantalla la deshabilita, pero aqui se vuelve
    // a revisar: el alta no se cae por eso, solo no se invita.
    public bool EnviarInvitacion { get; set; }

    [MinLength(1, ErrorMessage = "Debe agregar al menos un estudiante.")]
    public List<EstudianteClienteRequest> Estudiantes { get; set; } = new();
}