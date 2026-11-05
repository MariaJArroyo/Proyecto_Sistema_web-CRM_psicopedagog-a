using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// Los limites de texto son los de las columnas de TB_PLANTILLA_MENSAJE: si no
// coinciden, el error sale de MySQL como 500 en vez de como mensaje para la
// persona. Cuerpo es LONGTEXT y aqui se corta en 4000 a proposito, que es mas
// de lo que cabe en un correo razonable y evita que alguien pegue un libro.
public sealed record GuardarPlantillaRequest
{
    [Required(ErrorMessage = "Indique el nombre de la plantilla.")]
    [StringLength(100, ErrorMessage = "El nombre no puede pasar de 100 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [StringLength(200, ErrorMessage = "El asunto no puede pasar de 200 caracteres.")]
    public string? Asunto { get; init; }

    [Required(ErrorMessage = "Indique el contenido de la plantilla.")]
    [StringLength(4000, ErrorMessage = "El contenido no puede pasar de 4000 caracteres.")]
    public string Cuerpo { get; init; } = string.Empty;
}

public sealed record CambiarEstadoPlantillaRequest
{
    [Required(ErrorMessage = "Indique si la plantilla queda activa.")]
    public bool? Activa { get; init; }
}
