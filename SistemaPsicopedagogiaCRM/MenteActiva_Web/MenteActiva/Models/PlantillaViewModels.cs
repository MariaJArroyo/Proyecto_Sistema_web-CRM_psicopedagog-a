using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

// Calza con PlantillaResponse del API. EsSistema viene calculado desde alla.
public class PlantillaViewModel
{
    public int IdPlantilla { get; set; }

    public string? Clave { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Asunto { get; set; }

    public string Cuerpo { get; set; } = string.Empty;

    public bool Activa { get; set; }

    public bool EsSistema { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }
}

// Lo que la pantalla necesita al abrir: la lista de marcadores la manda el API
// para no repetirla en el JavaScript y que se desincronice con la validacion.
public class CatalogosPlantillaViewModel
{
    public List<string> Marcadores { get; set; } = [];
}

// Los mismos limites que GuardarPlantillaRequest del API: si aqui fueran mas
// anchos, el error llegaria del API en vez de salir en el modal.
public class GuardarPlantillaViewModel
{
    [Required(ErrorMessage = "Indique el nombre de la plantilla.")]
    [StringLength(100, ErrorMessage = "El nombre no puede pasar de 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "El asunto no puede pasar de 200 caracteres.")]
    public string? Asunto { get; set; }

    [Required(ErrorMessage = "Indique el contenido de la plantilla.")]
    [StringLength(4000, ErrorMessage = "El contenido no puede pasar de 4000 caracteres.")]
    public string Cuerpo { get; set; } = string.Empty;
}

public class CambiarEstadoPlantillaViewModel
{
    [Required(ErrorMessage = "Indique si la plantilla queda activa.")]
    public bool? Activa { get; set; }
}
