using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// Crear y editar comparten forma. Las reglas que dependen de la operacion (fecha
// en el pasado, lista de estudiantes obligatoria al crear) las revisa
// ServicioTareas.
public abstract class TareaBaseRequest
{
    // Material de apoyo, opcional. Tiene que existir y estar activo.
    public int? IdMaterial { get; set; }

    [Required(ErrorMessage = "Escriba el título de la tarea.")]
    [StringLength(150, ErrorMessage = "El título no puede pasar de 150 caracteres.")]
    public string? Titulo { get; set; }

    [StringLength(1000, ErrorMessage = "La descripción no puede pasar de 1000 caracteres.")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Indique la fecha límite de la tarea.")]
    public DateTime? FechaLimite { get; set; }
}

public sealed class CrearTareaRequest : TareaBaseRequest
{
    public List<int>? IdEstudiantes { get; set; }
}

public sealed class EditarTareaRequest : TareaBaseRequest
{
    // Nulo o vacio deja la asignacion como esta. No se puede quitar a todos los
    // estudiantes editando: para eso se elimina la tarea.
    public List<int>? IdEstudiantes { get; set; }
}
