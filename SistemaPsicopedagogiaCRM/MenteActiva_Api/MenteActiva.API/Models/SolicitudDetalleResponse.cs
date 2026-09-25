namespace MenteActiva.Api.Models;

public class SolicitudDetalleResponse
{
    public int IdSolicitud { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int? IdServicioInteres { get; set; }
    public string? Servicio { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? NotaInterna { get; set; }
    public int IdEstadoSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? IdEncargado { get; set; }
    public string? ClienteVinculado { get; set; }
    public string? AtendidaPor { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaAtencion { get; set; }

    // Clientes existentes con el mismo telefono o correo
    public List<ClienteCoincidenteResponse> Coincidencias { get; set; } = new();
}