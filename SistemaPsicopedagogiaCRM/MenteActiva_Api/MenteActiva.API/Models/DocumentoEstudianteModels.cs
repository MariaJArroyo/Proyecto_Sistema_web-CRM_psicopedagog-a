using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// =====================================================
// Documentos del estudiante (TB_DOCUMENTO_ESTUDIANTE)
// =====================================================

// multipart/form-data desde la ficha
public sealed class SubirDocumentoRequest
{
    // Vacio = el nombre del archivo
    [StringLength(150, ErrorMessage = "El título no puede pasar de 150 caracteres.")]
    public string? Titulo { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede pasar de 500 caracteres.")]
    public string? Descripcion { get; set; }

    public bool VisibleEnPortal { get; set; }

    public IFormFile? Archivo { get; set; }
}

public sealed record CambiarVisibilidadDocumentoRequest
{
    public bool Visible { get; init; }
}

// Lo que ve la ficha. Sin el nombre en disco.
public sealed class DocumentoEstudianteResponse
{
    public int IdDocumento { get; set; }
    public int IdEstudiante { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string TipoArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public bool VisibleEnPortal { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? SubidoPor { get; set; }
}

// Lo que ve la familia
public sealed class PortalDocumentoResponse
{
    public int IdDocumento { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string TipoArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public DateTime FechaCreacion { get; set; }
}

// Fila interna para servir el archivo
public sealed class DocumentoArchivoRegistro
{
    public int IdDocumento { get; set; }
    public string ArchivoNombre { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public string TipoArchivo { get; set; } = string.Empty;
}
