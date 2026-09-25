using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class SolicitudContactoRequest
{
    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{8}$")]
    public string Telefono { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Correo { get; set; } = string.Empty;

    public int? IdServicioInteres { get; set; }

    [Required, StringLength(2000)]
    public string Mensaje { get; set; } = string.Empty;

    // La manda la Web: el API solo ve la IP del servidor web, no la del visitante
    [StringLength(45)]
    public string? DireccionIp { get; set; }
}