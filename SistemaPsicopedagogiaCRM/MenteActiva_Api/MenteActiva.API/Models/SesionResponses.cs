namespace MenteActiva.Api.Models;

// Las propiedades tienen que llamarse igual que las columnas que devuelve el
// procedimiento: Dapper mapea por nombre y no avisa si una no calza, la deja
// en su valor por defecto.

// Una fila del historial. Trae tambien los campos de avance, para que la vista
// de historial de avances se arme con esta misma consulta (HU-M5-5).
public sealed class SesionListaResponse
{
    public int IdSesion { get; set; }
    public int IdEstudiante { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public int IdTipoSesion { get; set; }
    public string TipoSesion { get; set; } = string.Empty;
    public int IdTipoAtencion { get; set; }
    public string TipoAtencion { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string TemaTrabajado { get; set; } = string.Empty;
    public string? ObjetivosAlcanzados { get; set; }
    public string? HabilidadesDesarrolladas { get; set; }
    public string? Avances { get; set; }
    public string? Recomendaciones { get; set; }
    public string? Observaciones { get; set; }
    public int? IdCita { get; set; }
    public string RegistradaPor { get; set; } = string.Empty;
}

public sealed class SesionDetalleResponse
{
    public int IdSesion { get; set; }
    public int IdEstudiante { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public int IdTipoSesion { get; set; }
    public string TipoSesion { get; set; } = string.Empty;
    public int IdTipoAtencion { get; set; }
    public string TipoAtencion { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string TemaTrabajado { get; set; } = string.Empty;
    public string? ActividadesRealizadas { get; set; }
    public string? Acuerdos { get; set; }
    public string? ObjetivosAlcanzados { get; set; }
    public string? HabilidadesDesarrolladas { get; set; }
    public string? Avances { get; set; }
    public string? Recomendaciones { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    public int? IdCita { get; set; }
    public DateTime? FechaHoraCita { get; set; }
    public string RegistradaPor { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

// Cita a la que todavia se le puede registrar la sesion. Alimenta el campo
// opcional de cita de origen del formulario.
public sealed class CitaDisponibleSesionResponse
{
    public int IdCita { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public int IdTipoSesion { get; set; }
    public string TipoSesion { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public string EstadoCita { get; set; } = string.Empty;
}

public sealed class TipoAtencionResponse
{
    public int IdTipoAtencion { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// Los dos selects del formulario en una sola llamada, para no pedir dos veces.
public sealed class CatalogosSesionResponse
{
    public IReadOnlyList<TipoSesionResponse> TiposSesion { get; set; } = [];
    public IReadOnlyList<TipoAtencionResponse> TiposAtencion { get; set; } = [];
}
