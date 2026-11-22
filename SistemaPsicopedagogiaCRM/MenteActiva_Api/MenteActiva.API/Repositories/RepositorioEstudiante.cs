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

        // Encargado y parentesco.
        parametros.Add("p_IdEncargado", datos.IdEncargado);
        parametros.Add("p_IdParentesco", datos.IdParentesco);

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


    public Task DesactivarAsync(int idUsuarioAccion, int idEstudiante, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEstudiante", idEstudiante);

        return EjecutarAsync("SP_Estudiante_Eliminar", parametros, cancelacion);
    }

    #region Lookups: áreas de dificultad y necesidades de apoyo

    public Task<IReadOnlyList<OpcionCatalogoResponse>> BuscarAreasDificultadAsync(
        string? texto, CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_Texto", texto?.Trim());
        return ConsultarAsync<OpcionCatalogoResponse>("SP_BuscarAreasDificultad", p, cancelacion);
    }

    public Task<IReadOnlyList<OpcionCatalogoResponse>> BuscarNecesidadesApoyoAsync(
        string? texto, CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_Texto", texto?.Trim());
        return ConsultarAsync<OpcionCatalogoResponse>("SP_BuscarNecesidadesApoyo", p, cancelacion);
    }

    public Task<IReadOnlyList<NecesidadEstudianteResponse>> ListarNecesidadesAsync(
        int idEstudiante, CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_IdEstudiante", idEstudiante);
        return ConsultarAsync<NecesidadEstudianteResponse>(
            "SP_Estudiante_ListarNecesidades", p, cancelacion);
    }

    public async Task AgregarNecesidadAsync(
        int idEstudiante, string nombreNecesidad, string? observacion,
        CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_IdEstudiante", idEstudiante);
        p.Add("p_NombreNecesidad", nombreNecesidad.Trim());
        p.Add("p_Observacion", observacion?.Trim());
        await EjecutarAsync("SP_Estudiante_AgregarNecesidad", p, cancelacion);
    }


    public async Task EditarNecesidadAsync(
        int idEstudiante,
        int idNecesidadApoyo,
        string nombreNecesidad,
        string? observacion,
        CancellationToken cancelacion)
    {
        var p = new DynamicParameters();

        p.Add("p_IdEstudiante", idEstudiante);
        p.Add("p_IdNecesidadApoyo", idNecesidadApoyo);
        p.Add("p_NombreNecesidad", nombreNecesidad.Trim());
        p.Add("p_Observacion", observacion?.Trim());

        await EjecutarAsync(
            "SP_Estudiante_EditarNecesidad",
            p,
            cancelacion);
    }


    public async Task QuitarNecesidadAsync(
        int idEstudiante, int idNecesidadApoyo, CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_IdEstudiante", idEstudiante);
        p.Add("p_IdNecesidadApoyo", idNecesidadApoyo);
        await EjecutarAsync("SP_Estudiante_QuitarNecesidad", p, cancelacion);
    }

    #endregion

    #region Información académica adicional

    public Task<IReadOnlyList<InfoAcademicaResponse>> ListarInfoAcademicaAsync(
        int idEstudiante, CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_IdEstudiante", idEstudiante);
        return ConsultarAsync<InfoAcademicaResponse>(
            "SP_Estudiante_ListarInfoAcademica", p, cancelacion);
    }

    public async Task AgregarInfoAcademicaAsync(
        int idEstudiante, GuardarInfoAcademicaRequest datos, CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_IdEstudiante", idEstudiante);
        p.Add("p_Titulo", datos.Titulo.Trim());
        p.Add("p_Detalle", (datos.Detalle ?? string.Empty).Trim());
        await EjecutarAsync("SP_Estudiante_AgregarInfoAcademica", p, cancelacion);
    }

    public async Task EditarInfoAcademicaAsync(
        int idEstudiante, int idInfo, GuardarInfoAcademicaRequest datos,
        CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_IdEstudiante", idEstudiante);
        p.Add("p_IdInfoAcademica", idInfo);
        p.Add("p_Titulo", datos.Titulo.Trim());
        p.Add("p_Detalle", (datos.Detalle ?? string.Empty).Trim());
        await EjecutarAsync("SP_Estudiante_EditarInfoAcademica", p, cancelacion);
    }

    public async Task QuitarInfoAcademicaAsync(
        int idEstudiante, int idInfo, CancellationToken cancelacion)
    {
        var p = new DynamicParameters();
        p.Add("p_IdEstudiante", idEstudiante);
        p.Add("p_IdInfoAcademica", idInfo);
        await EjecutarAsync("SP_Estudiante_QuitarInfoAcademica", p, cancelacion);
    }


    public async Task ActualizarResumenAsync(
        int idEstudiante,
        string? resumen,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Resumen", resumen?.Trim());

        await EjecutarAsync(
            "SP_Estudiante_ActualizarResumen",
            parametros,
            cancelacion);
    }

    #endregion
}