namespace MenteActiva.Models;

public class PortalCitaViewModel
{
    public int IdCita { get; set; }

    public DateTime FechaHoraInicio { get; set; }

    public DateTime FechaHoraFin { get; set; }

    public string TipoSesion { get; set; } = string.Empty;

    public string Modalidad { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? ColorHex { get; set; }
}