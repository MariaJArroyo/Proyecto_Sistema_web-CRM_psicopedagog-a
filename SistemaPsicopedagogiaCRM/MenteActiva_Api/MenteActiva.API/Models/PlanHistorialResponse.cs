namespace MenteActiva.Api.Models;

public class PlanHistorialResponse
{
    public int IdBitacora { get; set; }
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
}