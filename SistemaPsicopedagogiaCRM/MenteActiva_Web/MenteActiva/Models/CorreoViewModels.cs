using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

// Calza con CatalogosCorreoResponse del API: todo lo que la pestania de correo
// necesita al abrir, en una sola llamada.
public class CatalogosComunicacionViewModel
{
    public List<DestinatarioCorreoViewModel> Destinatarios { get; set; } = [];

    public List<PlantillaViewModel> Plantillas { get; set; } = [];

    public List<string> Marcadores { get; set; } = [];
}

public class DestinatarioCorreoViewModel
{
    public int IdEncargado { get; set; }

    public string Encargado { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public List<EstudianteDestinatarioViewModel> Estudiantes { get; set; } = [];
}

public class EstudianteDestinatarioViewModel
{
    public int IdEncargado { get; set; }

    public int IdEstudiante { get; set; }

    public string Estudiante { get; set; } = string.Empty;
}

// Los mismos limites que EnviarCorreoRequest del API. Cuerpo se corta en 1000
// porque es el ancho de la columna donde queda guardado.
public class EnviarCorreoViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un destinatario.")]
    public int IdEncargado { get; set; }

    public int? IdEstudiante { get; set; }

    // La cita de la que habla el correo. Es la que resuelve [Fecha], [Hora] y
    // [Motivo], y solo vale junto con el estudiante.
    public int? IdCita { get; set; }

    [Required(ErrorMessage = "Indique el asunto del correo.")]
    [StringLength(200, ErrorMessage = "El asunto no puede pasar de 200 caracteres.")]
    public string Asunto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el mensaje del correo.")]
    [StringLength(1000, ErrorMessage = "El mensaje no puede pasar de 1000 caracteres.")]
    public string Cuerpo { get; set; } = string.Empty;
}
