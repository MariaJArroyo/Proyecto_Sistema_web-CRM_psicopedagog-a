using System.Text.Json.Serialization;

namespace MenteActiva.Api.Models;

// Fila de SP_Material_Listar y de la lectura por id. Los nombres coinciden con
// las columnas que devuelven las dos consultas.
public class MaterialResponse
{
    public int IdMaterial { get; set; }
    public int IdTipoMaterial { get; set; }
    public string TipoMaterial { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Url { get; set; }

    // Es el nombre GUID con que se guardo el PDF en disco. No sale en el JSON:
    // la pantalla solo necesita saber si hay archivo, y la descarga va por id.
    [JsonIgnore]
    public string? RutaArchivo { get; set; }

    public bool TieneArchivo => !string.IsNullOrEmpty(RutaArchivo);
    public bool Publicado { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public string RegistradoPor { get; set; } = string.Empty;
    public int? IdNivelEducativo { get; set; }
    public string? NivelEducativo { get; set; }
    public string? TipoActividad { get; set; }
    public DateTime? FechaActividad { get; set; }
    public bool Activo { get; set; }
}
