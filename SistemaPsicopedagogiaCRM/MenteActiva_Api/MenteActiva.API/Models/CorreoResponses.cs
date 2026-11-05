namespace MenteActiva.Api.Models;

// Un destinatario posible: encargado activo y con correo registrado. Los
// estudiantes vienen aparte y se cruzan por IdEncargado.
public sealed class DestinatarioCorreoResponse
{
    public int IdEncargado { get; init; }

    public string Encargado { get; init; } = string.Empty;

    public string Correo { get; init; } = string.Empty;

    // Se llena en el servicio con las filas del segundo resultado del SP
    public List<EstudianteDestinatarioResponse> Estudiantes { get; set; } = [];
}

public sealed class EstudianteDestinatarioResponse
{
    public int IdEncargado { get; init; }

    public int IdEstudiante { get; init; }

    public string Estudiante { get; init; } = string.Empty;
}

// Una cita del estudiante que se puede mencionar en un correo. Calza con
// SP_Correo_ListarCitasEstudiante.
//
// MotivoCancelacion viene lleno solo en las canceladas, y es lo unico que puede
// resolver [Motivo]: en TB_CITA no hay un motivo de la cita en si.
public sealed class CitaCorreoResponse
{
    public int IdCita { get; init; }

    public int IdEstudiante { get; init; }

    public DateTime FechaHoraInicio { get; init; }

    public string TipoSesion { get; init; } = string.Empty;

    public string Estado { get; init; } = string.Empty;

    public string? MotivoCancelacion { get; init; }
}

// Lo que necesita la pestania de correo al abrir: a quien se le puede escribir,
// que plantillas hay y que marcadores existen.
public sealed class CatalogosCorreoResponse
{
    public IReadOnlyList<DestinatarioCorreoResponse> Destinatarios { get; init; } = [];

    public IReadOnlyList<PlantillaResponse> Plantillas { get; init; } = [];

    public IReadOnlyList<string> Marcadores { get; init; } = [];
}

// El texto ya sustituido, antes de mandarlo. Si SinResolver trae algo, el envio
// se niega: es la regla de D-D.
public sealed class VistaPreviaCorreoResponse
{
    public string Asunto { get; init; } = string.Empty;

    public string Cuerpo { get; init; } = string.Empty;

    public IReadOnlyList<string> SinResolver { get; init; } = [];

    public bool SePuedeEnviar => SinResolver.Count == 0;

    // Para explicar el bloqueo sin que la pantalla arme el texto
    public string? Advertencia { get; init; }
}

// Calza con las columnas de SP_Notificacion_Listar.
public sealed class NotificacionResponse
{
    public int IdNotificacion { get; init; }

    public int? IdCita { get; init; }

    public int? IdEncargado { get; init; }

    public int? IdUsuarioRegistro { get; init; }

    // Nulo en el correo escrito a mano; con valor dice que aviso automatico es
    public string? ClavePlantilla { get; init; }

    public string? Destinatario { get; init; }

    public string? CorreoDestino { get; init; }

    public string Canal { get; init; } = string.Empty;

    // Nulo cuando la genero el sistema
    public string? Remitente { get; init; }

    public string? Asunto { get; init; }

    public string Cuerpo { get; init; } = string.Empty;

    public DateTime FechaProgramada { get; init; }

    public DateTime? FechaEnvio { get; init; }

    public bool Enviada { get; init; }

    public int Intentos { get; init; }

    public string? MensajeError { get; init; }

    // La escribio una persona si quedo dicho quien
    public bool EsManual => IdUsuarioRegistro is not null;
}

// Una fila lista para despachar. Calza con SP_Notificacion_ObtenerPendientes.
public sealed class NotificacionPendienteResponse
{
    public int IdNotificacion { get; init; }

    public int? IdCita { get; init; }

    // Nulo en el correo escrito a mano; con valor dice que aviso automatico es
    public string? ClavePlantilla { get; init; }

    public string? Destinatario { get; init; }

    public string Correo { get; init; } = string.Empty;

    public string Canal { get; init; } = string.Empty;

    public string? Asunto { get; init; }

    public string Cuerpo { get; init; } = string.Empty;

    public DateTime FechaProgramada { get; init; }

    public int Intentos { get; init; }
}

// Lo que devuelve el envio. IdNotificacion sirve para que la pantalla pueda
// mostrar la fila recien creada sin recargar todo.
public sealed class CorreoEnviadoResponse
{
    public int IdNotificacion { get; init; }

    public string CorreoDestino { get; init; } = string.Empty;

    public string Mensaje { get; init; } = string.Empty;
}
