using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class AsignarRolRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol.")]
    public int IdRol { get; set; }
}
