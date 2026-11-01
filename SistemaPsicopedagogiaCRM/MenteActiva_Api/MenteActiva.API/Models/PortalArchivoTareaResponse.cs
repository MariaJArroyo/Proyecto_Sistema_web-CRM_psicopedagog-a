namespace MenteActiva.Api.Models;

public class PortalArchivoTareaResponse
{
    public int IdTareaAsignacion { get; set; }

    public int IdTarea { get; set; }

    public string Tarea { get; set; } = string.Empty;

    public int IdMaterial { get; set; }

    public string Material { get; set; } = string.Empty;

    public string RutaArchivo { get; set; } = string.Empty;
}