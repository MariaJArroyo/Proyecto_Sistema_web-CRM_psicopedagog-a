namespace MenteActiva.Models;

// Pestana Documentos del portal: los documentos que la profesional marco
// como visibles para la familia
public class PortalDocumentosViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public int IdEstudiante { get; set; }

    public List<PortalDocumentoViewModel> Documentos { get; set; } = [];
}

public class PortalDocumentoViewModel
{
    public int IdDocumento { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;

    public string TipoArchivo { get; set; } = string.Empty;

    public long TamanoBytes { get; set; }

    public DateTime FechaCreacion { get; set; }
}
