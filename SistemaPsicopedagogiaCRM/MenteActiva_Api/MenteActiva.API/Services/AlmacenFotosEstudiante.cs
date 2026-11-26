namespace MenteActiva.Api.Services;

// Fotos de los estudiantes (ficha), en Archivos/FotosEstudiantes. Solo
// imagenes; la logica esta en AlmacenImagenesPdf.
public interface IAlmacenFotosEstudiante
{
    void CrearCarpeta();

    Task<(string Nombre, string Tipo)> GuardarAsync(Stream contenido, CancellationToken cancelacion);

    Stream? Abrir(string? nombreArchivo);

    void Borrar(string? nombreArchivo);
}

public sealed class AlmacenFotosEstudiante : AlmacenImagenesPdf, IAlmacenFotosEstudiante
{
    public const long TamanoMaximoBytes = 3 * 1024 * 1024;
    public const int TamanoMaximoMb = 3;

    public AlmacenFotosEstudiante(IWebHostEnvironment entorno, ILogger<AlmacenFotosEstudiante> registro)
        : base(entorno, registro, "FotosEstudiantes",
               "La foto debe ser una imagen JPG, PNG o WEBP.", soloImagenes: true)
    {
    }
}
