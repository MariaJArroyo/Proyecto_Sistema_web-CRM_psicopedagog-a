using MenteActiva.Api.Infraestructura;

namespace MenteActiva.Api.Services;

// Comprobantes de pago que suben las familias, en Archivos/Comprobantes dentro
// del ContentRootPath del API. Mismo esquema que AlmacenMateriales: en la base
// solo queda el nombre (un GUID) y el tipo, nunca una ruta.
//
// Se aceptan capturas (JPG, PNG, WEBP) y PDF. El tipo se decide por los
// primeros bytes del archivo, no por la extension ni por lo que diga el
// navegador, asi un .exe renombrado a .png no entra.
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

public sealed class AlmacenComprobantes : IAlmacenComprobantes
{
    public const long TamanoMaximoBytes = 5 * 1024 * 1024;
    public const int TamanoMaximoMb = 5;

    private static readonly (byte[] Firma, int Desfase, string Tipo, string Extension)[] Formatos =
    {
        (new byte[] { 0xFF, 0xD8, 0xFF }, 0, "image/jpeg", ".jpg"),
        (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, "image/png", ".png"),
        ("%PDF"u8.ToArray(), 0, "application/pdf", ".pdf"),
        // WEBP: "RIFF" .... "WEBP"; se revisa "WEBP" en el byte 8
        ("WEBP"u8.ToArray(), 8, "image/webp", ".webp")
    };

    private const int LargoCabecera = 12;

    private readonly string _carpeta;
    private readonly ILogger<AlmacenComprobantes> _registro;

    public AlmacenComprobantes(IWebHostEnvironment entorno, ILogger<AlmacenComprobantes> registro)
    {
        _carpeta = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, "Archivos", "Comprobantes"));
        _registro = registro;
    }

    public void CrearCarpeta() => Directory.CreateDirectory(_carpeta);

    public async Task<(string Nombre, string Tipo)> GuardarAsync(Stream contenido, CancellationToken cancelacion)
    {
        var cabecera = new byte[LargoCabecera];
        var leidos = await contenido.ReadAtLeastAsync(
            cabecera, cabecera.Length, throwOnEndOfStream: false, cancelacion);

        var formato = Formatos.FirstOrDefault(f =>
            leidos >= f.Desfase + f.Firma.Length
            && cabecera.AsSpan(f.Desfase, f.Firma.Length).SequenceEqual(f.Firma)
            // RIFF al inicio, para no confundir cualquier archivo con WEBP
            && (f.Tipo != "image/webp" || cabecera.AsSpan(0, 4).SequenceEqual("RIFF"u8)));

        if (formato.Tipo is null)
        {
            throw new ReglaNegocioException("El comprobante debe ser una imagen (JPG, PNG o WEBP) o un PDF.");
        }

        var nombre = $"{Guid.NewGuid():N}{formato.Extension}";
        var ruta = Path.Combine(_carpeta, nombre);

        try
        {
            await using var destino = new FileStream(
                ruta, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);

            // La cabecera ya se leyo del flujo, asi que se escribe aparte
            await destino.WriteAsync(cabecera.AsMemory(0, leidos), cancelacion);
            await contenido.CopyToAsync(destino, cancelacion);
        }
        catch
        {
            Borrar(nombre);
            throw;
        }

        return (nombre, formato.Tipo);
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
            _registro.LogWarning(ex, "No se pudo borrar el comprobante {Archivo}", nombreArchivo);
        }
    }

    // Igual que en AlmacenMateriales: solo un nombre de archivo, y la ruta
    // completa tiene que quedar dentro de la carpeta
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
