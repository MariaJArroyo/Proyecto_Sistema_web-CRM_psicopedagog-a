namespace MenteActiva.Api.Models;

// Fila de SP_Portal_ListarRecursos. No lleva RutaArchivo: el nombre en disco
// no sale del API. La descarga se pide por IdMaterial.
public sealed class PortalRecursoResponse
{
    public int IdMaterial { get; set; }
    public int IdTipoMaterial { get; set; }
    public string TipoMaterial { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Url { get; set; }
    public bool TieneArchivo { get; set; }
    public int? IdNivelEducativo { get; set; }
    public string? NivelEducativo { get; set; }
    public string? TipoActividad { get; set; }
    public DateTime? FechaActividad { get; set; }
    public DateTime? FechaPublicacion { get; set; }
}
