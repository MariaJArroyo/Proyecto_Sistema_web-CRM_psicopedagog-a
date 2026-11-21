namespace MenteActiva.Models;

public class PortalRecursoViewModel
{
    public int IdMaterial { get; set; }

    public int IdTipoMaterial { get; set; }

    public string TipoMaterial { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string? Url { get; set; }

    public bool TieneArchivo { get; set; }

    public string? NivelEducativo { get; set; }

    public string? TipoActividad { get; set; }

    public DateTime? FechaActividad { get; set; }

    public DateTime? FechaPublicacion { get; set; }
}

public class PortalRecursosViewModel
{
    public List<PortalEstudianteViewModel> Estudiantes { get; set; } = [];

    public int IdEstudiante { get; set; }

    public List<PortalRecursoViewModel> Recursos { get; set; } = [];
}
