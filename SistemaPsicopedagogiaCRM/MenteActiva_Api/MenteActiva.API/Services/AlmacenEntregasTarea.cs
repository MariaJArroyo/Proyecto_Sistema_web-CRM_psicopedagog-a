namespace MenteActiva.Api.Services;

// Lo que adjunta la familia al entregar una tarea (una foto del trabajo o un
// PDF), en Archivos/EntregasTareas. La logica esta en AlmacenImagenesPdf.
public interface IAlmacenEntregasTarea
{
    void CrearCarpeta();

    Task<(string Nombre, string Tipo)> GuardarAsync(Stream contenido, CancellationToken cancelacion);

    Stream? Abrir(string? nombreArchivo);

    void Borrar(string? nombreArchivo);
}

public sealed class AlmacenEntregasTarea : AlmacenImagenesPdf, IAlmacenEntregasTarea
{
    public const long TamanoMaximoBytes = 10 * 1024 * 1024;
    public const int TamanoMaximoMb = 10;

    public AlmacenEntregasTarea(IWebHostEnvironment entorno, ILogger<AlmacenEntregasTarea> registro)
        : base(entorno, registro, "EntregasTareas",
               "El archivo de la tarea debe ser una foto (JPG, PNG o WEBP) o un PDF.")
    {
    }
}
