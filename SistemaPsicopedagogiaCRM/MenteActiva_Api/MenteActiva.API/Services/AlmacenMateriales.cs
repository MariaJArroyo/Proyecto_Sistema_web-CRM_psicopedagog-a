using MenteActiva.Api.Infraestructura;

namespace MenteActiva.Api.Services;

// Singleton: no guarda estado aparte de la ruta de la carpeta. La carpeta se
// crea al arrancar (Program.cs); si en su lugar hay un archivo con ese nombre,
// el API no levanta, que es mejor que fallar en la primera subida.
public sealed class AlmacenMateriales : IAlmacenMateriales
{
    private static readonly byte[] FirmaPdf = "%PDF"u8.ToArray();

    private readonly string _carpeta;
    private readonly ILogger<AlmacenMateriales> _registro;

    public AlmacenMateriales(IWebHostEnvironment entorno, ILogger<AlmacenMateriales> registro)
    {
        _carpeta = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, "Archivos", "Materiales"));
        _registro = registro;
    }

    public void CrearCarpeta() => Directory.CreateDirectory(_carpeta);

    public async Task<string> GuardarPdfAsync(Stream contenido, CancellationToken cancelacion)
    {
        var cabecera = new byte[FirmaPdf.Length];
        var leidos = await contenido.ReadAtLeastAsync(
            cabecera, cabecera.Length, throwOnEndOfStream: false, cancelacion);

        if (leidos < cabecera.Length || !cabecera.AsSpan().SequenceEqual(FirmaPdf))
        {
            throw new ReglaNegocioException("El archivo no es un PDF válido.");
        }

        var nombre = $"{Guid.NewGuid():N}.pdf";
        var ruta = Path.Combine(_carpeta, nombre);

        try
        {
            await using var destino = new FileStream(
                ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);

            // La cabecera ya se leyo del flujo, asi que se escribe aparte
            await destino.WriteAsync(cabecera, cancelacion);
            await contenido.CopyToAsync(destino, cancelacion);
        }
        catch
        {
            Borrar(nombre);
            throw;
        }

        return nombre;
    }

    public ArchivoDescarga? AbrirParaDescarga(string? nombreArchivo, string titulo)
    {
        var ruta = RutaSegura(nombreArchivo);

        if (ruta is null)
        {
            return null;
        }

        var informacion = new FileInfo(ruta);

        if (!informacion.Exists || informacion.LinkTarget is not null)
        {
            return null;
        }

        try
        {
            var flujo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new ArchivoDescarga(flujo, NombreDescarga(titulo));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public void Borrar(string nombreArchivo)
    {
        var ruta = RutaSegura(nombreArchivo);

        if (ruta is null)
        {
            return;
        }

        try
        {
            File.Delete(ruta);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _registro.LogWarning(ex, "No se pudo borrar el archivo de material {Archivo}", nombreArchivo);
        }
    }

    // Unico control de nombre de archivo: lo usan la descarga del personal, la
    // de recursos y la de tareas del portal. La ultima comprobacion asegura que
    // la ruta completa sigue dentro de la carpeta
    private string? RutaSegura(string? nombreArchivo)
    {
        var nombre = nombreArchivo?.Trim();

        if (string.IsNullOrWhiteSpace(nombre)
            || nombre is "." or ".."
            || Path.IsPathRooted(nombre)
            || nombre.IndexOfAny(new[] { '/', '\\', ':' }) >= 0
            || nombre.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        var ruta = Path.GetFullPath(Path.Combine(_carpeta, nombre));

        return Path.GetDirectoryName(ruta) == _carpeta ? ruta : null;
    }

    // El titulo es texto libre: se le quitan los caracteres que no puede
    // llevar un nombre de archivo y se le pone la extension
    private static string NombreDescarga(string titulo)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = new string(titulo.Select(c => invalidos.Contains(c) ? '_' : c).ToArray()).Trim();

        if (limpio.Length == 0)
        {
            limpio = "material";
        }

        return limpio.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? limpio : limpio + ".pdf";
    }
}
