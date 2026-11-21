using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

public interface IServicioDocumentos
{
    Task<int> SubirAsync(int idUsuarioAccion, int idEstudiante, SubirDocumentoRequest peticion, CancellationToken cancelacion);

    Task<IReadOnlyList<DocumentoEstudianteResponse>> ListarAsync(int idEstudiante, CancellationToken cancelacion);

    Task<DocumentoDescarga?> AbrirAsync(int idDocumento, CancellationToken cancelacion);

    Task CambiarVisibilidadAsync(int idUsuarioAccion, int idDocumento, bool visible, CancellationToken cancelacion);

    Task EliminarAsync(int idUsuarioAccion, int idDocumento, CancellationToken cancelacion);

    Task<IReadOnlyList<PortalDocumentoResponse>> ListarPortalAsync(
        int idUsuario, int idEstudiante, CancellationToken cancelacion);

    Task<DocumentoDescarga?> AbrirPortalAsync(
        int idUsuario, int idEstudiante, int idDocumento, CancellationToken cancelacion);
}

public sealed record DocumentoDescarga(Stream Contenido, string Tipo, string NombreDescarga);

// Lo que la base no puede revisar: el archivo (tamanio y formato real) y el
// nombre que trae del equipo de quien lo sube. El resto lo validan las SPs.
public sealed class ServicioDocumentos : IServicioDocumentos
{
    private readonly IRepositorioDocumento _repositorio;
    private readonly IAlmacenDocumentos _almacen;

    public ServicioDocumentos(IRepositorioDocumento repositorio, IAlmacenDocumentos almacen)
    {
        _repositorio = repositorio;
        _almacen = almacen;
    }

    public async Task<int> SubirAsync(
        int idUsuarioAccion, int idEstudiante, SubirDocumentoRequest peticion, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException("Seleccione un estudiante válido.");
        }

        var archivo = peticion.Archivo;

        if (archivo is null || archivo.Length == 0)
        {
            throw new ReglaNegocioException("Seleccione un archivo.");
        }

        if (archivo.Length > AlmacenDocumentos.TamanoMaximoBytes)
        {
            throw new ReglaNegocioException($"El archivo no puede pasar de {AlmacenDocumentos.TamanoMaximoMb} MB.");
        }

        var nombreOriginal = NombreLimpio(archivo.FileName);
        var titulo = string.IsNullOrWhiteSpace(peticion.Titulo)
            ? Path.GetFileNameWithoutExtension(nombreOriginal)
            : peticion.Titulo.Trim();

        await using var flujo = archivo.OpenReadStream();
        var (nombre, tipo) = await _almacen.GuardarAsync(flujo, nombreOriginal, cancelacion);

        try
        {
            return await _repositorio.CrearAsync(
                idUsuarioAccion, idEstudiante, titulo,
                string.IsNullOrWhiteSpace(peticion.Descripcion) ? null : peticion.Descripcion.Trim(),
                nombre, nombreOriginal, tipo, archivo.Length, peticion.VisibleEnPortal, cancelacion);
        }
        catch
        {
            _almacen.Borrar(nombre);
            throw;
        }
    }

    public Task<IReadOnlyList<DocumentoEstudianteResponse>> ListarAsync(int idEstudiante, CancellationToken cancelacion)
        => _repositorio.ListarPorEstudianteAsync(idEstudiante, cancelacion);

    public async Task<DocumentoDescarga?> AbrirAsync(int idDocumento, CancellationToken cancelacion)
        => Abrir(await _repositorio.ObtenerArchivoAsync(idDocumento, cancelacion));

    public Task CambiarVisibilidadAsync(
        int idUsuarioAccion, int idDocumento, bool visible, CancellationToken cancelacion)
        => _repositorio.CambiarVisibilidadAsync(idUsuarioAccion, idDocumento, visible, cancelacion);

    public Task EliminarAsync(int idUsuarioAccion, int idDocumento, CancellationToken cancelacion)
        => _repositorio.EliminarAsync(idUsuarioAccion, idDocumento, cancelacion);

    public Task<IReadOnlyList<PortalDocumentoResponse>> ListarPortalAsync(
        int idUsuario, int idEstudiante, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException("Seleccione un estudiante válido.");
        }

        return _repositorio.ListarPortalAsync(idUsuario, idEstudiante, cancelacion);
    }

    public async Task<DocumentoDescarga?> AbrirPortalAsync(
        int idUsuario, int idEstudiante, int idDocumento, CancellationToken cancelacion)
        => Abrir(await _repositorio.ObtenerArchivoPortalAsync(idUsuario, idEstudiante, idDocumento, cancelacion));

    private DocumentoDescarga? Abrir(DocumentoArchivoRegistro? registro)
    {
        if (registro is null)
        {
            return null;
        }

        var flujo = _almacen.Abrir(registro.ArchivoNombre);

        return flujo is null
            ? null
            : new DocumentoDescarga(flujo, registro.TipoArchivo, registro.NombreOriginal);
    }

    // Solo el nombre, sin carpetas ni caracteres que un sistema de archivos no acepte
    private static string NombreLimpio(string? nombre)
    {
        var soloNombre = Path.GetFileName(nombre ?? string.Empty);
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = new string(soloNombre.Select(c => invalidos.Contains(c) ? '_' : c).ToArray()).Trim();

        return string.IsNullOrEmpty(limpio) ? "documento" : limpio.Length > 255 ? limpio[^255..] : limpio;
    }
}
