using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// Los mensajes van escritos porque [ApiController] los devuelve tal cual al
// navegador: sin ellos sale el texto por defecto de .NET, en ingles.
public class CambiarEstadoSolicitudRequest
{
    [Range(1, 4, ErrorMessage = "Seleccione un estado válido para la solicitud.")]
    public int IdEstadoSolicitud { get; set; }

    [StringLength(1000, ErrorMessage = "La nota interna no puede pasar de 1000 caracteres.")]
    public string? NotaInterna { get; set; }
}