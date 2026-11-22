namespace MenteActiva.Models;

public class PortalTareaViewModel
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

    public DateTime? FechaRevision { get; set; }

    // Lo que adjunto la familia al entregar (foto o PDF)
    public bool TieneArchivoEntrega { get; set; }

    public string? NombreArchivoEntrega { get; set; }

    // Se puede entregar mientras falte hacerlo, la hayan devuelto para corregir
    // o ya se entrego pero la profesional todavia no la revisa
    public bool SePuedeEntregar => Estado.ToLowerInvariant() is "asignada" or "vencida" or "por corregir" or "entregada";

    public string? Material { get; set; }

    public string? MaterialUrl { get; set; }

    public bool TieneArchivo { get; set; }
}