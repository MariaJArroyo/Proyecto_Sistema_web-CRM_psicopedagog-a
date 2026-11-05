using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class VincularSolicitudRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar el cliente al que se vincula.")]
    public int IdEncargado { get; set; }
}