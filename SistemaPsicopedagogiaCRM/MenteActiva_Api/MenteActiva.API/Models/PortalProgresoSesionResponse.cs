namespace MenteActiva.Api.Models;

public class PortalProgresoSesionResponse
{
    public int IdSesion { get; set; }

    public DateTime Fecha { get; set; }

    public string TipoAtencion { get; set; } = string.Empty;

    public string TemaTrabajado { get; set; } = string.Empty;

    public string? Avances { get; set; }

    public string? Recomendaciones { get; set; }
}