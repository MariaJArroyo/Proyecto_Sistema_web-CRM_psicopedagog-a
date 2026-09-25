using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class VincularSolicitudRequest
{
    [Range(1, int.MaxValue)]
    public int IdEncargado { get; set; }
}