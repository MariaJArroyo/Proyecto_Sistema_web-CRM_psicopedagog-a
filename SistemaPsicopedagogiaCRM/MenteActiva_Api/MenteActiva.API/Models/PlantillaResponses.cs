namespace MenteActiva.Api.Models;

// Las propiedades calzan con las columnas que devuelven SP_Plantilla_Listar y
// SP_Plantilla_ObtenerDetalle. Si cambia una columna, cambia esto.
public sealed class PlantillaResponse
{
    public int IdPlantilla { get; init; }

    // Llena solo en las plantillas que usa el sistema. No se muestra en la
    // pantalla: lo que se muestra es EsSistema.
    public string? Clave { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? Asunto { get; init; }

    public string Cuerpo { get; init; } = string.Empty;

    public bool Activa { get; init; }

    public DateTime FechaCreacion { get; init; }

    public DateTime? FechaModificacion { get; init; }

    // Se calcula aqui y no en la base: el SP devolveria un entero y mapearlo a
    // bool depende del driver. Con la Clave a mano, la cuenta es trivial.
    public bool EsSistema => Clave is not null;
}

// Lo que necesita la pantalla al abrir la pestania de plantillas: las plantillas
// y la lista de marcadores que se pueden usar, para no repetirla en el
// JavaScript y que se desincronice.
public sealed class CatalogosPlantillaResponse
{
    public IReadOnlyList<string> Marcadores { get; init; } = [];
}
