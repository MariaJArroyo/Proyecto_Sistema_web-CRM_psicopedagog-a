namespace MenteActiva.Api.Models;

public class PortalTareaResponse
{
    public int IdTareaAsignacion { get; set; }

    public int IdTarea { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime FechaLimite { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaEntrega { get; set; }

    public string? ComentarioEstudiante { get; set; }

    public string? ComentarioRevision { get; set; }

    public string? Material { get; set; }

    public string? MaterialUrl { get; set; }

    public bool TieneArchivo { get; set; }
}