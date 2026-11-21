using Dapper;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Lo unico que habla con la base. Las reglas de datos y la bitacora estan en
// los procedimientos; la validacion de la peticion, en el servicio.
public sealed class RepositorioTarea : RepositorioBase, IRepositorioTarea
{
    public RepositorioTarea(IConfiguration config)
        : base(config)
    {
    }

    public Task<IReadOnlyList<TareaResponse>> ListarAsync(CancellationToken cancelacion)
        => ConsultarAsync<TareaResponse>("SP_Tarea_Listar", new DynamicParameters(), cancelacion);

    // SP_Tarea_Obtener devuelve dos resultados: la tarea y sus asignados.
    public async Task<TareaDetalleResponse?> ObtenerAsync(int idTarea, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdTarea", idTarea);

        try
        {
            return await ConsultarVariosAsync(
                "SP_Tarea_Obtener",
                parametros,
                async lector =>
                {
                    var tarea = await lector.ReadFirstOrDefaultAsync<TareaDetalleResponse>();

                    if (tarea is null)
                    {
                        return null;
                    }

                    tarea.Estudiantes = (await lector.ReadAsync<TareaAsignadoResponse>()).AsList();

                    return tarea;
                },
                cancelacion);
        }
        catch (ReglaNegocioException)
        {
            // La unica regla de este procedimiento es "la tarea no existe"
            return null;
        }
    }

    public Task<int> CrearAsync(
        int idUsuarioAccion, int? idMaterial, string titulo, string? descripcion,
        DateTime fechaLimite, string estudiantesJson, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdMaterial", idMaterial);
        parametros.Add("p_Titulo", titulo);
        parametros.Add("p_Descripcion", descripcion);
        parametros.Add("p_FechaLimite", fechaLimite.Date);
        parametros.Add("p_EstudiantesJson", estudiantesJson);

        return EscalarAsync("SP_Tarea_Crear", parametros, cancelacion);
    }

    public Task EditarAsync(
        int idUsuarioAccion, int idTarea, int? idMaterial, string titulo, string? descripcion,
        DateTime fechaLimite, string? estudiantesJson, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdTarea", idTarea);
        parametros.Add("p_IdMaterial", idMaterial);
        parametros.Add("p_Titulo", titulo);
        parametros.Add("p_Descripcion", descripcion);
        parametros.Add("p_FechaLimite", fechaLimite.Date);
        parametros.Add("p_EstudiantesJson", estudiantesJson);

        return EjecutarAsync("SP_Tarea_Editar", parametros, cancelacion);
    }

    public Task EliminarAsync(int idUsuarioAccion, int idTarea, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdTarea", idTarea);

        return EjecutarAsync("SP_Tarea_Eliminar", parametros, cancelacion);
    }
}
