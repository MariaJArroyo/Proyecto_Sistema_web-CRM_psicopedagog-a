namespace MenteActiva.Api.Models;

public sealed class AreaEstadoResponse
{
    public int IdAreaDificultad { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Observacion { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaDesactivacion { get; set; }
}