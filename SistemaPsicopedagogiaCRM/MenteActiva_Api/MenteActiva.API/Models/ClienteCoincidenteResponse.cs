namespace MenteActiva.Api.Models;

public class ClienteCoincidenteResponse
{
    public int Id { get; set; }
    public string Encargado { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string Estado { get; set; } = string.Empty;
}