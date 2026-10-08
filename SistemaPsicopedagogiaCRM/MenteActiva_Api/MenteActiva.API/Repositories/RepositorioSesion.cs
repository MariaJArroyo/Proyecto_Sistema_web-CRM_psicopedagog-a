using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Lo unico que llama procedimientos de sesiones. Sin reglas de negocio: la
// conexion y la traduccion de SIGNAL '45000' a ReglaNegocioException viven en
// RepositorioBase.
public sealed class RepositorioSesion : RepositorioBase, IRepositorioSesion
{
    public RepositorioSesion(IConfiguration config)
        : base(config)
    {
    }

    // ---------- consultas ----------

    public Task<IReadOnlyList<SesionListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde, DateOnly? hasta, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Desde", desde?.ToDateTime(TimeOnly.MinValue).Date);
        parametros.Add("p_Hasta", hasta?.ToDateTime(TimeOnly.MinValue).Date);

        return ConsultarAsync<SesionListaResponse>("SP_Sesion_Listar", parametros, cancelacion);
    }

    public async Task<SesionDetalleResponse?> ObtenerAsync(int idSesion, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdSesion", idSesion);

        var filas = await ConsultarAsync<SesionDetalleResponse>(
            "SP_Sesion_ObtenerDetalle", parametros, cancelacion);

        return filas.Count > 0 ? filas[0] : null;
    }

    public Task<IReadOnlyList<CitaDisponibleSesionResponse>> ListarCitasDisponiblesAsync(
        int idEstudiante, DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Ahora", ahora);

        return ConsultarAsync<CitaDisponibleSesionResponse>(
            "SP_Sesion_ListarCitasDisponibles", parametros, cancelacion);
    }

    public Task<IReadOnlyList<EstudianteBusquedaResponse>> BuscarEstudiantesAsync(
        string texto, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Texto", texto);

        return ConsultarAsync<EstudianteBusquedaResponse>(
            "SP_Cita_BuscarEstudiantes_CRM", parametros, cancelacion);
    }

    public Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion)
        => ConsultarAsync<TipoSesionResponse>("SP_ListarTiposSesion_CRM", null, cancelacion);

    public Task<IReadOnlyList<TipoAtencionResponse>> ListarTiposAtencionAsync(CancellationToken cancelacion)
        => ConsultarAsync<TipoAtencionResponse>("SP_TipoAtencion_Listar", null, cancelacion);

    // ---------- escrituras ----------

    public Task<int> RegistrarAsync(
        int idUsuarioAccion, RegistrarSesionRequest peticion, DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEstudiante", peticion.IdEstudiante);
        parametros.Add("p_IdCita", peticion.IdCita);
        parametros.Add("p_IdTipoSesion", peticion.IdTipoSesion);
        parametros.Add("p_IdTipoAtencion", peticion.IdTipoAtencion);
        parametros.Add("p_FechaHoraInicio", peticion.FechaHoraInicio);
        parametros.Add("p_DuracionMinutos", peticion.DuracionMinutos);
        parametros.Add("p_Objetivo", peticion.Objetivo);
        parametros.Add("p_TemaTrabajado", peticion.TemaTrabajado);
        parametros.Add("p_ActividadesRealizadas", peticion.ActividadesRealizadas);
        parametros.Add("p_Acuerdos", peticion.Acuerdos);
        parametros.Add("p_ObjetivosAlcanzados", peticion.ObjetivosAlcanzados);
        parametros.Add("p_HabilidadesDesarrolladas", peticion.HabilidadesDesarrolladas);
        parametros.Add("p_Avances", peticion.Avances);
        parametros.Add("p_Recomendaciones", peticion.Recomendaciones);
        parametros.Add("p_Observaciones", peticion.Observaciones);
        parametros.Add("p_Ahora", ahora);

        return EscalarAsync("SP_Sesion_Registrar", parametros, cancelacion);
    }

    public Task EditarAsync(
        int idUsuarioAccion, int idSesion, EditarSesionRequest peticion, DateTime ahora,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdSesion", idSesion);
        parametros.Add("p_IdTipoSesion", peticion.IdTipoSesion);
        parametros.Add("p_IdTipoAtencion", peticion.IdTipoAtencion);
        parametros.Add("p_FechaHoraInicio", peticion.FechaHoraInicio);
        parametros.Add("p_DuracionMinutos", peticion.DuracionMinutos);
        parametros.Add("p_Objetivo", peticion.Objetivo);
        parametros.Add("p_TemaTrabajado", peticion.TemaTrabajado);
        parametros.Add("p_ActividadesRealizadas", peticion.ActividadesRealizadas);
        parametros.Add("p_Acuerdos", peticion.Acuerdos);
        parametros.Add("p_ObjetivosAlcanzados", peticion.ObjetivosAlcanzados);
        parametros.Add("p_HabilidadesDesarrolladas", peticion.HabilidadesDesarrolladas);
        parametros.Add("p_Avances", peticion.Avances);
        parametros.Add("p_Recomendaciones", peticion.Recomendaciones);
        parametros.Add("p_Observaciones", peticion.Observaciones);
        parametros.Add("p_Ahora", ahora);

        return EjecutarAsync("SP_Sesion_Editar", parametros, cancelacion);
    }

    public Task InactivarAsync(
        int idUsuarioAccion, int idSesion, DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdSesion", idSesion);
        parametros.Add("p_Ahora", ahora);

        return EjecutarAsync("SP_Sesion_Inactivar", parametros, cancelacion);
    }
}
