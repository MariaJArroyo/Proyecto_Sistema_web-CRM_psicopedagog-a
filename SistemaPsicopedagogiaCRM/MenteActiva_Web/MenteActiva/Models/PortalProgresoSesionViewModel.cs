namespace MenteActiva.Models;

public class PortalProgresoSesionViewModel
{
    public int IdSesion { get; set; }

    public DateTime Fecha { get; set; }

    public string TipoAtencion { get; set; } = string.Empty;

    public string TemaTrabajado { get; set; } = string.Empty;

    public string? Avances { get; set; }

    public string? Recomendaciones { get; set; }
}