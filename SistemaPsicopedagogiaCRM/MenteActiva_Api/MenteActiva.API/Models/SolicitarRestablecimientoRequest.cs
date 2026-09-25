using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class SolicitarRestablecimientoRequest
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = string.Empty;
}
