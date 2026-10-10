
namespace MenteActiva.API.Models;

public class ActividadCrearRequest
{
    public int IdEstrategia { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
