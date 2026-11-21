namespace MenteActiva.Api.Models;

// Una fila de la tabla administrativa (SP_Tarea_Listar)
public sealed class TareaResponse
{
    public int IdTarea { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime FechaLimite { get; set; }

    public int? IdMaterial { get; set; }

    public string? Material { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int Asignados { get; set; }

    public int Entregaron { get; set; }
}

// Un estudiante asignado a la tarea (segundo resultado de SP_Tarea_Obtener)
public sealed class TareaAsignadoResponse
{
    public int IdTareaAsignacion { get; set; }

    public int IdEstudiante { get; set; }

    public string Estudiante { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaEntrega { get; set; }
}

// La tarea con sus asignados, para llenar el formulario de edicion
public sealed class TareaDetalleResponse
{
    public int IdTarea { get; set; }

    public int? IdMaterial { get; set; }

    public string? Material { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime FechaLimite { get; set; }

    public DateTime FechaCreacion { get; set; }

    public IReadOnlyList<TareaAsignadoResponse> Estudiantes { get; set; } = Array.Empty<TareaAsignadoResponse>();
}
