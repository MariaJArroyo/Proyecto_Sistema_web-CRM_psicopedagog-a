using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class ClienteRegistroRequest : ClienteRequest
{
    public int? IdSolicitud { get; set; }

    [MinLength(1, ErrorMessage = "Debe agregar al menos un estudiante.")]
    public List<EstudianteClienteRequest> Estudiantes { get; set; } = new();
}