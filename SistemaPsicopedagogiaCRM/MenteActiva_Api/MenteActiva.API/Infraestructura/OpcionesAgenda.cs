using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Infraestructura;

// Seccion "Agenda" de appsettings.json. Se valida al arrancar: si falta algo,
// el API no levanta en vez de fallar a medio uso.
public sealed class OpcionesAgenda
{
    public const string Seccion = "Agenda";

    [Required]
    public string ZonaHoraria { get; set; } = "America/Costa_Rica";

    [Range(1, 60)]
    public int MinutosRevisionVencidas { get; set; } = 5;
}