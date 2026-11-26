namespace MenteActiva.Api.Models;

// Un renglon de la bandeja. Calza con SP_Comunicacion_ListarHilos.
public sealed class HiloResponse
{
    public int IdHilo { get; init; }

    public string Asunto { get; init; } = string.Empty;

    // Para el personal, el usuario del encargado. Para el encargado, 0: su
    // contraparte es el consultorio y no una persona.
    public int IdContraparte { get; init; }

    // Con quien es la conversacion, visto desde quien pregunta
    public string Contraparte { get; init; } = string.Empty;

    public string UltimoMensaje { get; init; } = string.Empty;

    public DateTime UltimaFecha { get; init; }

    public int SinLeer { get; init; }
}

// Un mensaje dentro de un hilo. Calza con SP_Mensaje_AbrirHilo.
public sealed class MensajeResponse
{
    public int IdMensaje { get; init; }

    public int IdUsuarioRemitente { get; init; }

    public string Remitente { get; init; } = string.Empty;

    public bool RemitenteEsEncargado { get; init; }

    public string Asunto { get; init; } = string.Empty;

    public string Cuerpo { get; init; } = string.Empty;

    public bool Leido { get; init; }

    public DateTime FechaEnvio { get; init; }

    public DateTime? FechaLectura { get; init; }

    // Lo escribio quien esta mirando. La pantalla lo usa para alinear la burbuja.
    public bool Mio { get; init; }
}

public sealed class DestinatarioMensajeResponse
{
    public int IdUsuario { get; init; }

    public string NombreCompleto { get; init; } = string.Empty;

    // Para que el encargado sepa a quien le escribe
    public string Roles { get; init; } = string.Empty;
}

// Un rol del personal con su marca de acceso a la bandeja del consultorio.
// Calza con SP_Mensajeria_ListarAcceso.
public sealed class AccesoMensajeriaRolResponse
{
    public int IdRol { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public bool TieneAcceso { get; init; }
}

public sealed class MensajeEnviadoResponse
{
    public int IdMensaje { get; init; }

    public int IdHilo { get; init; }

    public string Mensaje { get; init; } = string.Empty;
}
