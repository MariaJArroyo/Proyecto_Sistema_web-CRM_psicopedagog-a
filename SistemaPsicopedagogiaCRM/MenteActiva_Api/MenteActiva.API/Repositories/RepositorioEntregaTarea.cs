using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioEntregaTarea
{
    // Devuelve el archivo que quedo reemplazado (para borrarlo) o null
    Task<string?> EntregarAsync(
        int idUsuario, int idEstudiante, int idTareaAsignacion, string? comentario,
        string? archivo, string? tipoArchivo, string? nombreArchivo, CancellationToken cancelacion);

    Task<IReadOnlyList<EntregaTareaResponse>> ListarPorTareaAsync(int idTarea, CancellationToken cancelacion);

    Task<ArchivoEntregaRegistro?> ObtenerArchivoAsync(int idTareaAsignacion, CancellationToken cancelacion);

    Task<ArchivoEntregaRegistro?> ObtenerArchivoPortalAsync(
        int idUsuario, int idEstudiante, int idTareaAsignacion, CancellationToken cancelacion);

    Task RevisarAsync(
        int idUsuarioAccion, int idTareaAsignacion, bool aprobada, string? comentario, CancellationToken cancelacion);
}

// Solo llama SPs: a quien pertenece la tarea y en que estado se puede
// entregar o revisar lo deciden los procedimientos.
public sealed class RepositorioEntregaTarea : RepositorioBase, IRepositorioEntregaTarea
{
    public RepositorioEntregaTarea(IConfiguration config)
        : base(config)
    {
    }

    public async Task<string?> EntregarAsync(
        int idUsuario, int idEstudiante, int idTareaAsignacion, string? comentario,
        string? archivo, string? tipoArchivo, string? nombreArchivo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdTareaAsignacion", idTareaAsignacion);
        parametros.Add("p_Comentario", comentario);
        parametros.Add("p_Archivo", archivo);
        parametros.Add("p_TipoArchivo", tipoArchivo);
        parametros.Add("p_NombreArchivo", nombreArchivo);

        var filas = await ConsultarAsync<string?>("SP_TareaAsignacion_Entregar", parametros, cancelacion);

        return filas.FirstOrDefault();
    }

    public Task<IReadOnlyList<EntregaTareaResponse>> ListarPorTareaAsync(int idTarea, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdTarea", idTarea);

        return ConsultarAsync<EntregaTareaResponse>("SP_Tarea_ListarEntregas", parametros, cancelacion);
    }

    public async Task<ArchivoEntregaRegistro?> ObtenerArchivoAsync(int idTareaAsignacion, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdTareaAsignacion", idTareaAsignacion);

        var filas = await ConsultarAsync<ArchivoEntregaRegistro>(
            "SP_TareaAsignacion_ObtenerArchivoEntrega", parametros, cancelacion);

        return filas.FirstOrDefault();
    }

    public async Task<ArchivoEntregaRegistro?> ObtenerArchivoPortalAsync(
        int idUsuario, int idEstudiante, int idTareaAsignacion, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdTareaAsignacion", idTareaAsignacion);

        var filas = await ConsultarAsync<ArchivoEntregaRegistro>(
            "SP_Portal_ObtenerArchivoEntrega", parametros, cancelacion);

        return filas.FirstOrDefault();
    }

    public Task RevisarAsync(
        int idUsuarioAccion, int idTareaAsignacion, bool aprobada, string? comentario, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdTareaAsignacion", idTareaAsignacion);
        parametros.Add("p_Aprobada", aprobada);
        parametros.Add("p_Comentario", comentario);

        return EjecutarAsync("SP_TareaAsignacion_Revisar", parametros, cancelacion);
    }
}
