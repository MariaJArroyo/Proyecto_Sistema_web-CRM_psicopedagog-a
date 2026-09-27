namespace MenteActiva.Api.Models;

public class CambiarAccesoRequest
{
    // En falso reactiva. Es un booleano y no un estado porque desde esta
    // pantalla solo existen esas dos salidas.
    public bool Suspender { get; set; }
}
