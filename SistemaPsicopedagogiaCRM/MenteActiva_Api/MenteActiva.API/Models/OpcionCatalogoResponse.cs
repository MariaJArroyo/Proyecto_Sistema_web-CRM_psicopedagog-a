namespace MenteActiva.Api.Models;

public sealed class OpcionCatalogoResponse
{
    public string Nombre { get; set; } = string.Empty;
}

public sealed class NecesidadEstudianteResponse
{
    public int IdNecesidadApoyo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Observacion { get; set; }
}