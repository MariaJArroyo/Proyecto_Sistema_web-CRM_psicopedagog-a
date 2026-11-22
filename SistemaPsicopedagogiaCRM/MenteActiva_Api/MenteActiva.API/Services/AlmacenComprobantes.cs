namespace MenteActiva.Api.Services;

// Comprobantes de pago que suben las familias, en Archivos/Comprobantes.
// La logica (formatos, nombres GUID, rutas seguras) esta en AlmacenImagenesPdf.
public interface IAlmacenComprobantes
{
    void CrearCarpeta();

    // Valida el contenido, lo guarda y devuelve (nombre, tipo MIME).
    // Lanza ReglaNegocioException si no es un formato aceptado.
    Task<(string Nombre, string Tipo)> GuardarAsync(Stream contenido, CancellationToken cancelacion);

    // null si el nombre no es valido o el archivo ya no esta
    Stream? Abrir(string? nombreArchivo);

    void Borrar(string? nombreArchivo);
}

public sealed class AlmacenComprobantes : AlmacenImagenesPdf, IAlmacenComprobantes
{
    public const long TamanoMaximoBytes = 5 * 1024 * 1024;
    public const int TamanoMaximoMb = 5;

    public AlmacenComprobantes(IWebHostEnvironment entorno, ILogger<AlmacenComprobantes> registro)
        : base(entorno, registro, "Comprobantes",
               "El comprobante debe ser una imagen (JPG, PNG o WEBP) o un PDF.")
    {
    }
}
