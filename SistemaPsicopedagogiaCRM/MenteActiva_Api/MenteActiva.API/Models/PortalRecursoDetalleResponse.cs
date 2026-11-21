namespace MenteActiva.Api.Models;

// Fila de SP_Portal_ObtenerRecurso. Uso interno del API: RutaArchivo es el
// nombre en disco y no se devuelve en ninguna respuesta.
public sealed class PortalRecursoDetalleResponse
{
    public int IdMaterial { get; set; }
    public int IdTipoMaterial { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? RutaArchivo { get; set; }
}
