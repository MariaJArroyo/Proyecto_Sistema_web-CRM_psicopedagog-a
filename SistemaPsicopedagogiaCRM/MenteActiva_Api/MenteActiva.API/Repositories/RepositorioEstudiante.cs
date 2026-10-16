using Dapper;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public class RepositorioEstudiante : RepositorioBase
{
    public RepositorioEstudiante(IConfiguration config)
        : base(config)
    {
    }

    public async Task AgregarAreaAsync(
        int idEstudiante,
        string nombreArea,
        string? observacion,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_NombreArea", nombreArea.Trim());
        parametros.Add("p_Observacion", observacion?.Trim());

        await EjecutarAsync(
            "SP_Estudiante_AgregarArea_CRM",
            parametros,
            cancelacion);
    }

    public async Task EditarAreaAsync(
        int idEstudiante,
        int idAreaDificultad,
        string nombreArea,
        string? observacion,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdAreaDificultad", idAreaDificultad);
        parametros.Add("p_NombreArea", nombreArea.Trim());
        parametros.Add("p_Observacion", observacion?.Trim());

        await EjecutarAsync(
            "SP_Estudiante_EditarArea_CRM",
            parametros,
            cancelacion);
    }

    public async Task EditarFichaAsync(
        int idUsuarioAccion,
        int idEstudiante,
        EditarEstudianteRequest datos,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Nombre", datos.Nombre.Trim());
        parametros.Add("p_PrimerApellido", datos.PrimerApellido.Trim());
        parametros.Add("p_SegundoApellido", datos.SegundoApellido?.Trim());
        parametros.Add("p_FechaNacimiento", datos.FechaNacimiento?.Date);
        parametros.Add("p_IdNivelEducativo", datos.IdNivelEducativo);
        parametros.Add("p_NombreInstitucion", datos.Institucion?.Trim());
        parametros.Add("p_Observaciones", datos.Observaciones?.Trim());
        parametros.Add("p_NecesidadesApoyo", datos.NecesidadesApoyo?.Trim());

        // El SP en la base se llama SP_Estudiante_EditarFicha (sin _CRM)
        await EjecutarAsync(
            "SP_Estudiante_EditarFicha",
            parametros,
            cancelacion);
    }

    public async Task<IReadOnlyList<EncargadoBusquedaResponse>> BuscarEncargadosAsync(
        string? texto,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_Texto", texto?.Trim());

        return await ConsultarAsync<EncargadoBusquedaResponse>(
            "SP_BuscarEncargados",
            parametros,
            cancelacion);
    }

    public async Task<int> CrearConEncargadoAsync(
        int idUsuarioAccion,
        NuevoEstudianteRequest datos,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_Nombre", datos.Nombre.Trim());
        parametros.Add("p_PrimerApellido", datos.PrimerApellido.Trim());
        parametros.Add("p_SegundoApellido", datos.SegundoApellido?.Trim());
        parametros.Add("p_FechaNacimiento", datos.FechaNacimiento?.Date);
        parametros.Add("p_IdNivelEducativo", datos.IdNivelEducativo);
        parametros.Add("p_NombreInstitucion", datos.Institucion?.Trim());
        parametros.Add("p_Observaciones", datos.Observaciones?.Trim());
        parametros.Add("p_IdEncargado", datos.IdEncargado);
        parametros.Add("p_IdParentesco", datos.IdParentesco);

        return await EscalarAsync(
            "SP_Estudiante_CrearConEncargado",
            parametros,
            cancelacion);
    }
    #region Listar áreas actuales e inactivas

    public Task<IReadOnlyList<AreaEstadoResponse>> ListarAreasEstadoAsync(
        int idEstudiante,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdEstudiante", idEstudiante);

        return ConsultarAsync<AreaEstadoResponse>(
            "SP_Estudiante_ListarAreasEstado",
            parametros,
            cancelacion);
    }

    #endregion

    #region Desactivar o reactivar área

    public Task CambiarEstadoAreaAsync(
        int idEstudiante,
        int idAreaDificultad,
        bool activo,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdAreaDificultad", idAreaDificultad);
        parametros.Add("p_Activo", activo);

        return EjecutarAsync(
            "SP_Estudiante_CambiarEstadoArea",
            parametros,
            cancelacion);
    }

    #endregion
}