using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// La familia entrega una tarea desde el portal (multipart/form-data)
public sealed class EntregarTareaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante válido.")]
    public int IdEstudiante { get; set; }

    [StringLength(500, ErrorMessage = "El comentario no puede pasar de 500 caracteres.")]
    public string? Comentario { get; set; }

    // Foto o PDF del trabajo (opcional)
    public IFormFile? Archivo { get; set; }
}

// La profesional aprueba la entrega o la devuelve para corregir
public sealed class RevisarEntregaRequest
{
    public bool Aprobada { get; set; } = true;

    [StringLength(500, ErrorMessage = "El comentario no puede pasar de 500 caracteres.")]
    public string? Comentario { get; set; }
}

// Una fila de SP_Tarea_ListarEntregas: un estudiante asignado y su entrega
public sealed class EntregaTareaResponse
{
    public int IdTareaAsignacion { get; set; }

    public int IdEstudiante { get; set; }

    public string Estudiante { get; set; } = string.Empty;

    public int IdEstadoTarea { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaEntrega { get; set; }

    public bool EntregaTardia { get; set; }

    public string? ComentarioEstudiante { get; set; }

    public string? ComentarioRevision { get; set; }

    public DateTime? FechaRevision { get; set; }

    public string? RevisadoPor { get; set; }

    public bool TieneArchivo { get; set; }

    public string? TipoArchivo { get; set; }

    public string? NombreArchivo { get; set; }
}

// Lo que se necesita para leer el archivo entregado del disco
public sealed class ArchivoEntregaRegistro
{
    public string? ArchivoEntrega { get; set; }

    public string? TipoArchivo { get; set; }

    public string? NombreArchivo { get; set; }
}
