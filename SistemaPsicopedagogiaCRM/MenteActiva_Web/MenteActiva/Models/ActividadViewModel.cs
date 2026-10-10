
namespace MenteActiva.Models;

public class ActividadViewModel
{
    public int IdActividad { get; set; }

    public int IdEstrategia { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Completada { get; set; }

    public DateTime FechaCreacion { get; set; }
}
