using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class CambiarEstadoSolicitudRequest
{
    [Range(1, 4)]
    public int IdEstadoSolicitud { get; set; }

    [StringLength(1000)]
    public string? NotaInterna { get; set; }
}