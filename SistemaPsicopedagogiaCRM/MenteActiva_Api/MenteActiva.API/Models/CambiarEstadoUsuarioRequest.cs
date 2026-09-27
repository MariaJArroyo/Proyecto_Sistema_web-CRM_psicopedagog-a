using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// 1 Activo, 2 Inactivo, 3 Bloqueado, 4 Pendiente de activacion
public class CambiarEstadoUsuarioRequest
{
    [Required]
    [Range(1, 4, ErrorMessage = "El estado indicado no existe.")]
    public int IdEstadoUsuario { get; set; }
}
