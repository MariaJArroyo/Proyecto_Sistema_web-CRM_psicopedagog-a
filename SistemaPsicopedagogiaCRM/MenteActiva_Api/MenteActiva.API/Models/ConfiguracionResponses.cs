namespace MenteActiva.Api.Models;

// ---------- Filas tal como salen de las SPs (solo las usa el repositorio/servicio) ----------

public sealed record FranjaHorarioFila
{
    public int IdHorarioAtencion { get; init; }
    public int DiaSemana { get; init; }
    public TimeSpan HoraInicio { get; init; }
    public TimeSpan HoraFin { get; init; }
}

public sealed record DiaNoLaboralFila
{
    public int IdDiaNoLaboral { get; init; }
    public DateTime Fecha { get; init; }
    public string Motivo { get; init; } = string.Empty;
    public bool EsFeriado { get; init; }
}

public sealed record ConfiguracionFila
{
    public string Clave { get; init; } = string.Empty;
    public string Valor { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
}

// ---------- Respuestas del API ----------

public sealed record FranjaHorarioResponse
{
    public int DiaSemana { get; init; }
    public string HoraInicio { get; init; } = string.Empty;
    public string HoraFin { get; init; } = string.Empty;
}

public sealed record CitaFueraDeHorarioResponse
{
    public int IdCita { get; init; }
    public int? IdGrupoCita { get; init; }
    public string Estudiante { get; init; } = string.Empty;
    public DateTime FechaHoraInicio { get; init; }
    public DateTime FechaHoraFin { get; init; }
}

public sealed record GuardarHorarioResponse(
    string Mensaje,
    IReadOnlyList<CitaFueraDeHorarioResponse> CitasFueraDeHorario);

public sealed record DiaNoLaboralResponse
{
    public int IdDiaNoLaboral { get; init; }
    public DateOnly Fecha { get; init; }
    public string Motivo { get; init; } = string.Empty;
    public bool EsFeriado { get; init; }
}

public sealed record DatosConsultorioResponse
{
    public string NombreConsultorio { get; init; } = string.Empty;
    public string CorreoContacto { get; init; } = string.Empty;
    public string TelefonoContacto { get; init; } = string.Empty;
    public string DireccionConsultorio { get; init; } = string.Empty;
    public string Moneda { get; init; } = string.Empty;
    public int HorasRecordatorioCita { get; init; }
}