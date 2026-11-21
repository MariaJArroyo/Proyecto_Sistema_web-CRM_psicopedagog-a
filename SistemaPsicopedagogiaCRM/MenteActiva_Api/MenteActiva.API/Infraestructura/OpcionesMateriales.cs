using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Infraestructura;

// Seccion "Materiales" de appsettings.json. Se valida al arrancar.
public sealed class OpcionesMateriales
{
    public const string Seccion = "Materiales";

    // Tamanio maximo de un PDF subido. El limite de la peticion se calcula de
    // aqui y solo se aplica al endpoint de subida (LimiteSubidaPdf).
    [Range(1, 100)]
    public int TamanoMaximoMb { get; set; } = 10;

    public long TamanoMaximoBytes => TamanoMaximoMb * 1024L * 1024L;
}
