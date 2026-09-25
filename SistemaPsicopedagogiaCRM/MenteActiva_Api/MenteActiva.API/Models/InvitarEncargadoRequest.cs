using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class InvitarEncargadoRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int IdEncargado { get; set; }
}
