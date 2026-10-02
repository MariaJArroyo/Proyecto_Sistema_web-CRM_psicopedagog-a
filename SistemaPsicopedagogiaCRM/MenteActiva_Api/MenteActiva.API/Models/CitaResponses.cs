namespace MenteActiva.Api.Models;

// Fila tal como la devuelve SP_Cita_ConsultarAgenda_CRM
public record CitaAgendaFila
{
    public int IdCita { get; init; }
    public int IdEstudiante { get; init; }
    public string Estudiante { get; init; } = string.Empty;
    public string? Encargado { get; init; }
    public DateTime FechaHoraInicio { get; init; }
    public DateTime FechaHoraFin { get; init; }
    public int IdTipoSesion { get; init; }
    public string TipoSesion { get; init; } = string.Empty;
    public int IdModalidad { get; init; }
    public string Modalidad { get; init; } = string.Empty;
    public int IdEstadoCita { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string? Observaciones { get; init; }
    public string? MotivoCancelacion { get; init; }
    public bool TieneSesion { get; init; }
    public int? IdGrupoCita { get; init; }
    public string? NombreGrupo { get; init; }
    public int? CupoGrupo { get; init; }
}

// Lo que recibe la pantalla: la fila mas las acciones permitidas. Las decide
// el servicio, asi la regla vive en un solo lugar y el JS solo muestra botones.
public sealed record CitaAgendaResponse : CitaAgendaFila
{
    public CitaAgendaResponse(CitaAgendaFila fila)
        : base(fila)
    {
    }

    public bool PuedeReprogramar { get; init; }
    public bool PuedeCancelar { get; init; }
    public bool PuedeMarcarNoAsistio { get; init; }

    public bool PuedeGestionarGrupo { get; init; }
}

// Lo minimo para reprogramar o cancelar (sale de SP_Cita_ObtenerDetalle)
public sealed record CitaResumenFila
{
    public int IdCita { get; init; }
    public int IdTipoSesion { get; init; }
    public int IdEstadoCita { get; init; }
    public DateTime FechaHoraInicio { get; init; }
    public DateTime FechaHoraFin { get; init; }
}

public sealed record EstudianteBusquedaResponse
{
    public int IdEstudiante { get; init; }
    public string Estudiante { get; init; } = string.Empty;
    public string? NivelEducativo { get; init; }
    public string? Encargado { get; init; }
}

public sealed record EspacioDisponibleResponse
{
    public DateTime Inicio { get; init; }
    public DateTime Fin { get; init; }
}

public sealed record TipoSesionResponse
{
    public int IdTipoSesion { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int DuracionMinutos { get; init; }
}

public sealed record ModalidadResponse
{
    public int IdModalidad { get; init; }
    public string Nombre { get; init; } = string.Empty;
}

public sealed record CatalogosAgendaResponse(
    IReadOnlyList<TipoSesionResponse> TiposSesion,
    IReadOnlyList<ModalidadResponse> Modalidades);

// Lo minimo para reprogramar un grupo (sale de SP_Grupo_Obtener_CRM)
public sealed record GrupoResumenFila
{
    public int IdGrupoCita { get; init; }
    public int IdTipoSesion { get; init; }
    public DateTime FechaHoraInicio { get; init; }
    public bool Activo { get; init; }
}

// Grupo de un dia que todavia acepta estudiantes
public sealed record GrupoDisponibleResponse
{
    public int IdGrupoCita { get; init; }
    public string? Nombre { get; init; }
    public DateTime Inicio { get; init; }
    public DateTime Fin { get; init; }
    public string TipoSesion { get; init; } = string.Empty;
    public string Modalidad { get; init; } = string.Empty;
    public int CupoMaximo { get; init; }
    public int Inscritos { get; init; }
}