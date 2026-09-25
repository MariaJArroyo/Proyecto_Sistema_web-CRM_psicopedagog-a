namespace MenteActiva.Api.Models;

public class SolicitudResponse
{
    public int IdSolicitud { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Servicio { get; set; }
    public int IdEstadoSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? IdEncargado { get; set; }
    public string? AtendidaPor { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaAtencion { get; set; }
}