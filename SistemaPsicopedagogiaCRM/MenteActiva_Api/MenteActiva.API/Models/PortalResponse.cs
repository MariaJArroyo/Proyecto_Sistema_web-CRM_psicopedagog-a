namespace MenteActiva.Api.Models;

public  class PortalResponse
{
    public List<PortalEstudianteResponse> Estudiantes { get; set; } = [];

    public PortalResumenResponse Resumen { get; set; } = new();
}

