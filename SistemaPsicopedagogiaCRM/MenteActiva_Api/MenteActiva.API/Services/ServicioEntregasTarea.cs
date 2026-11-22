using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

public interface IServicioEntregasTarea
{
    Task EntregarAsync(
        int idUsuario, int idTareaAsignacion, EntregarTareaRequest peticion, CancellationToken cancelacion);

    Task<IReadOnlyList<EntregaTareaResponse>> ListarPorTareaAsync(int idTarea, CancellationToken cancelacion);

    Task<ComprobanteDescarga?> AbrirArchivoAsync(int idTareaAsignacion, CancellationToken cancelacion);

    Task<ComprobanteDescarga?> AbrirArchivoPortalAsync(
        int idUsuario, int idEstudiante, int idTareaAsignacion, CancellationToken cancelacion);

    Task RevisarAsync(
        int idUsuarioAccion, int idTareaAsignacion, RevisarEntregaRequest peticion, CancellationToken cancelacion);
}

// Entregas de tareas. Aqui va lo que la base no puede revisar (el archivo:
// tamanio y formato) y el orden de guardar en disco y en la base sin dejar
// archivos huerfanos. Lo demas (de quien es la tarea, en que estado se puede
// entregar o revisar) lo validan las SPs.
public sealed class ServicioEntregasTarea : IServicioEntregasTarea
{
    private readonly IRepositorioEntregaTarea _repositorio;
    private readonly IAlmacenEntregasTarea _almacen;

    public ServicioEntregasTarea(IRepositorioEntregaTarea repositorio, IAlmacenEntregasTarea almacen)
    {
        _repositorio = repositorio;
        _almacen = almacen;
    }

    public async Task EntregarAsync(
        int idUsuario, int idTareaAsignacion, EntregarTareaRequest peticion, CancellationToken cancelacion)
    {
        if (idTareaAsignacion <= 0 || peticion.IdEstudiante <= 0)
        {
            throw new ReglaNegocioException("Seleccione una tarea válida.");
        }

        var archivo = peticion.Archivo is { Length: > 0 } ? peticion.Archivo : null;

        if (archivo is not null && archivo.Length > AlmacenEntregasTarea.TamanoMaximoBytes)
        {
            throw new ReglaNegocioException(
                $"El archivo no puede pasar de {AlmacenEntregasTarea.TamanoMaximoMb} MB.");
        }

        string? nombre = null;
        string? tipo = null;

        if (archivo is not null)
        {
            await using var flujo = archivo.OpenReadStream();
            (nombre, tipo) = await _almacen.GuardarAsync(flujo, cancelacion);
        }

        string? anterior;

        try
        {
            anterior = await _repositorio.EntregarAsync(
                idUsuario, peticion.IdEstudiante, idTareaAsignacion,
                Limpiar(peticion.Comentario), nombre, tipo,
                NombreVisible(archivo?.FileName), cancelacion);
        }
        catch
        {
            // Si la base no acepta la entrega, el archivo nuevo no se queda huerfano
            _almacen.Borrar(nombre);
            throw;
        }

        // La entrega anterior (por ejemplo, la que se devolvio para corregir)
        // ya no la referencia nadie
        _almacen.Borrar(anterior);
    }

    public Task<IReadOnlyList<EntregaTareaResponse>> ListarPorTareaAsync(int idTarea, CancellationToken cancelacion)
    {
        if (idTarea <= 0)
        {
            throw new ReglaNegocioException("Seleccione una tarea válida.");
        }

        return _repositorio.ListarPorTareaAsync(idTarea, cancelacion);
    }

    public async Task<ComprobanteDescarga?> AbrirArchivoAsync(int idTareaAsignacion, CancellationToken cancelacion)
        => Abrir(await _repositorio.ObtenerArchivoAsync(idTareaAsignacion, cancelacion), idTareaAsignacion);

    public async Task<ComprobanteDescarga?> AbrirArchivoPortalAsync(
        int idUsuario, int idEstudiante, int idTareaAsignacion, CancellationToken cancelacion)
        => Abrir(await _repositorio.ObtenerArchivoPortalAsync(
            idUsuario, idEstudiante, idTareaAsignacion, cancelacion), idTareaAsignacion);

    public Task RevisarAsync(
        int idUsuarioAccion, int idTareaAsignacion, RevisarEntregaRequest peticion, CancellationToken cancelacion)
    {
        var comentario = Limpiar(peticion.Comentario);

        if (!peticion.Aprobada && comentario is null)
        {
            throw new ReglaNegocioException("Escriba qué debe corregir: la familia lo verá en el portal.");
        }

        return _repositorio.RevisarAsync(idUsuarioAccion, idTareaAsignacion, peticion.Aprobada, comentario, cancelacion);
    }

    // ---------- auxiliares ----------

    private ComprobanteDescarga? Abrir(ArchivoEntregaRegistro? registro, int idTareaAsignacion)
    {
        if (registro?.ArchivoEntrega is null)
        {
            return null;
        }

        var flujo = _almacen.Abrir(registro.ArchivoEntrega);

        if (flujo is null)
        {
            return null;
        }

        var tipo = string.IsNullOrWhiteSpace(registro.TipoArchivo) ? "application/octet-stream" : registro.TipoArchivo;
        var extension = Path.GetExtension(registro.ArchivoEntrega);

        return new ComprobanteDescarga(flujo, tipo, $"tarea-{idTareaAsignacion}{extension}");
    }

    private static string? Limpiar(string? texto)
    {
        var limpio = texto?.Trim();
        return string.IsNullOrEmpty(limpio) ? null : limpio;
    }

    // Solo para mostrarlo en pantalla: sin carpetas y con un largo razonable.
    // El archivo en disco siempre se llama con un GUID.
    private static string? NombreVisible(string? nombre)
    {
        var limpio = Path.GetFileName(nombre?.Trim() ?? string.Empty);

        if (string.IsNullOrEmpty(limpio))
        {
            return null;
        }

        return limpio.Length <= 255 ? limpio : limpio[^255..];
    }
}
