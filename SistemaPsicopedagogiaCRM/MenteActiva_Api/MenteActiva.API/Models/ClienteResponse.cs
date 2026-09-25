namespace MenteActiva.Api.Models;

public class ClienteResponse
{
    public int Id { get; set; }
    public string Encargado { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Servicio { get; set; }
    public string? Estudiante { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int IdEstadoCliente { get; set; }
    public int CantidadEstudiantes { get; set; }
}
