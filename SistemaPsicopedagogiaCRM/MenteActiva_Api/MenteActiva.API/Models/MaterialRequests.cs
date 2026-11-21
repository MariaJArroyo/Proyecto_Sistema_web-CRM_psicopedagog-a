using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// Lo que comparten los cuatro tipos de material. Las reglas que dependen del
// tipo (nivel obligatorio en la guia, url en el enlace, tipo y fecha en la
// actividad) las revisa ServicioMateriales, porque aqui no se sabe de que tipo
// es el material que se edita.
public abstract class MaterialBaseRequest
{
    [Required(ErrorMessage = "Indique el título.")]
    [StringLength(150, ErrorMessage = "El título no puede pasar de 150 caracteres.")]
    public string? Titulo { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede pasar de 500 caracteres.")]
    public string? Descripcion { get; set; }

    public int? IdNivelEducativo { get; set; }
}

// Publicar en false (lo normal) deja el material como borrador
public sealed class CrearGuiaRequest : MaterialBaseRequest
{
    public bool Publicar { get; set; }
}

public sealed class CrearEnlaceRequest : MaterialBaseRequest
{
    [Required(ErrorMessage = "Indique la dirección del enlace.")]
    [StringLength(500, ErrorMessage = "La dirección no puede pasar de 500 caracteres.")]
    public string? Url { get; set; }

    public bool Publicar { get; set; }
}

public sealed class CrearActividadRequest : MaterialBaseRequest
{
    // Ludica, Cognitiva o Motriz, sin tilde: es lo que acepta el CHECK de la tabla
    [Required(ErrorMessage = "Seleccione el tipo de actividad.")]
    public string? TipoActividad { get; set; }

    [Required(ErrorMessage = "Indique la fecha de la actividad.")]
    public DateTime? FechaActividad { get; set; }

    public bool Publicar { get; set; }
}

// Una sola forma para editar los cuatro tipos. El servicio usa solo los campos
// que le tocan al tipo del material: Url en el enlace, TipoActividad y
// FechaActividad en la actividad. El tipo y el archivo no se cambian por aqui.
public sealed class EditarMaterialRequest : MaterialBaseRequest
{
    [StringLength(500, ErrorMessage = "La dirección no puede pasar de 500 caracteres.")]
    public string? Url { get; set; }

    public string? TipoActividad { get; set; }

    public DateTime? FechaActividad { get; set; }
}

public sealed class PublicacionMaterialRequest
{
    [Required(ErrorMessage = "Indique si el material queda publicado.")]
    public bool? Publicado { get; set; }
}

// Subida de PDF (multipart/form-data). No hereda de MaterialBaseRequest porque
// aca el titulo es opcional: si no viene, se usa el nombre original del
// archivo sin la extension. El tamanio y el %PDF los revisa el servicio.
public sealed class CrearPdfRequest
{
    public IFormFile? Archivo { get; set; }

    [StringLength(150, ErrorMessage = "El título no puede pasar de 150 caracteres.")]
    public string? Titulo { get; set; }

    [StringLength(500, ErrorMessage = "La descripción no puede pasar de 500 caracteres.")]
    public string? Descripcion { get; set; }

    public int? IdNivelEducativo { get; set; }

    public bool Publicar { get; set; }
}
