namespace MenteActiva.Api.Models;

public class CambiarContrasenaRequest
{
    public string ContrasenaActual { get; set; } = string.Empty;

    public string ContrasenaNueva { get; set; } = string.Empty;
}