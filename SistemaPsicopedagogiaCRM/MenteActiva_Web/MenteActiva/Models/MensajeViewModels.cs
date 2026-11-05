using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

// Calzan con los response del API. Los usan las dos pantallas, la del panel y
// la del portal: la mensajeria es la misma de los dos lados.
public class HiloViewModel
{
    public int IdHilo { get; set; }

    public string Asunto { get; set; } = string.Empty;

    public int IdContraparte { get; set; }

    public string Contraparte { get; set; } = string.Empty;

    public string UltimoMensaje { get; set; } = string.Empty;

    public DateTime UltimaFecha { get; set; }

    public int SinLeer { get; set; }
}

public class MensajeViewModel
{
    public int IdMensaje { get; set; }

    public int IdUsuarioRemitente { get; set; }

    public string Remitente { get; set; } = string.Empty;

    public bool RemitenteEsEncargado { get; set; }

    public string Asunto { get; set; } = string.Empty;

    public string Cuerpo { get; set; } = string.Empty;

    public bool Leido { get; set; }

    public DateTime FechaEnvio { get; set; }

    public DateTime? FechaLectura { get; set; }

    public bool Mio { get; set; }
}

public class DestinatarioMensajeViewModel
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Roles { get; set; } = string.Empty;
}

// Sin IdHilo abre una conversacion nueva; con IdHilo responde.
public class EnviarMensajeViewModel
{
    public int? IdHilo { get; set; }

    public int? IdUsuarioDestinatario { get; set; }

    [StringLength(200, ErrorMessage = "El asunto no puede pasar de 200 caracteres.")]
    public string? Asunto { get; set; }

    [Required(ErrorMessage = "Escriba el mensaje.")]
    [StringLength(4000, ErrorMessage = "El mensaje no puede pasar de 4000 caracteres.")]
    public string Cuerpo { get; set; } = string.Empty;
}

// La pantalla de mensajes del portal. Lleva Estudiantes e IdEstudiante aunque no
// los use: los necesita _NavbarPortal para su selector y para que los demas
// enlaces no pierdan el estudiante elegido.
public class PortalMensajesViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public int IdEstudiante { get; set; }

    public List<DestinatarioMensajeViewModel> Destinatarios { get; set; } = [];
}
