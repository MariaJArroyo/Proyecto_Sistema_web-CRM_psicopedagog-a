using MenteActiva.Api.Infraestructura;

namespace MenteActiva.Api.Services;

// Documentos de los estudiantes en Archivos/Documentos dentro del
// ContentRootPath del API. Igual que materiales y comprobantes: en la base
// queda un nombre GUID, nunca una ruta.
//
// El formato se decide por los primeros bytes. Los de Office nuevos (docx,
// xlsx, pptx) son un ZIP por dentro, asi que para ellos ademas se mira la
// extension del nombre original; un ZIP con otra extension no entra.
public interface IAlmacenDocumentos
{
    void CrearCarpeta();

    // Valida y guarda. Devuelve el nombre en disco y el tipo MIME.
    Task<(string Nombre, string Tipo)> GuardarAsync(
        Stream contenido, string nombreOriginal, CancellationToken cancelacion);

    Stream? Abrir(string? nombreArchivo);

    void Borrar(string? nombreArchivo);
}

public sealed class AlmacenDocumentos : IAlmacenDocumentos
{
    public const long TamanoMaximoBytes = 15 * 1024 * 1024;
    public const int TamanoMaximoMb = 15;

    private const int LargoCabecera = 12;

    private static readonly byte[] FirmaZip = { 0x50, 0x4B, 0x03, 0x04 };
    private static readonly byte[] FirmaOle = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

    private static readonly Dictionary<string, string> TiposOfficeZip = new(StringComparer.OrdinalIgnoreCase)
    {
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    private static readonly Dictionary<string, string> TiposOfficeViejo = new(StringComparer.OrdinalIgnoreCase)
    {
        [".doc"] = "application/msword",
        [".xls"] = "application/vnd.ms-excel",
        [".ppt"] = "application/vnd.ms-powerpoint"
    };

    private readonly string _carpeta;
    private readonly ILogger<AlmacenDocumentos> _registro;

    public AlmacenDocumentos(IWebHostEnvironment entorno, ILogger<AlmacenDocumentos> registro)
    {
        _carpeta = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, "Archivos", "Documentos"));
        _registro = registro;
    }

    public void CrearCarpeta() => Directory.CreateDirectory(_carpeta);

    public async Task<(string Nombre, string Tipo)> GuardarAsync(
        Stream contenido, string nombreOriginal, CancellationToken cancelacion)
    {
        var cabecera = new byte[LargoCabecera];
        var leidos = await contenido.ReadAtLeastAsync(
            cabecera, cabecera.Length, throwOnEndOfStream: false, cancelacion);

        var (tipo, extension) = Detectar(cabecera.AsSpan(0, leidos), Path.GetExtension(nombreOriginal ?? string.Empty));

        if (tipo is null)
        {
            throw new ReglaNegocioException(
                "Formato no permitido. Suba un PDF, una imagen (JPG, PNG, WEBP) o un documento de Word, Excel o PowerPoint.");
        }

        var nombre = $"{Guid.NewGuid():N}{extension}";
        var ruta = Path.Combine(_carpeta, nombre);

        try
        {
            await using var destino = new FileStream(
                ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);

            await destino.WriteAsync(cabecera.AsMemory(0, leidos), cancelacion);
            await contenido.CopyToAsync(destino, cancelacion);
        }
        catch
        {
            Borrar(nombre);
            throw;
        }

        return (nombre, tipo);
    }

    private static (string? Tipo, string Extension) Detectar(ReadOnlySpan<byte> c, string extensionOriginal)
    {
        if (c.StartsWith("%PDF"u8)) return ("application/pdf", ".pdf");
        if (c.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return ("image/jpeg", ".jpg");
        if (c.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ("image/png", ".png");
        if (c.Length >= 12 && c[..4].SequenceEqual("RIFF"u8) && c.Slice(8, 4).SequenceEqual("WEBP"u8))
            return ("image/webp", ".webp");

        var ext = extensionOriginal.ToLowerInvariant();

        if (c.StartsWith(FirmaZip) && TiposOfficeZip.TryGetValue(ext, out var tipoZip))
            return (tipoZip, ext);

        if (c.StartsWith(FirmaOle) && TiposOfficeViejo.TryGetValue(ext, out var tipoOle))
            return (tipoOle, ext);

        return (null, string.Empty);
    }

    public Stream? Abrir(string? nombreArchivo)
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
            return new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public void Borrar(string? nombreArchivo)
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
            _registro.LogWarning(ex, "No se pudo borrar el documento {Archivo}", nombreArchivo);
        }
    }

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
}
