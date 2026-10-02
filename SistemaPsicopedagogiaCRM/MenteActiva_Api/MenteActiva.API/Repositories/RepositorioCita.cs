using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Lo unico que llama procedimientos de citas. Aqui no hay reglas de negocio:
// la conexion y la traduccion de SIGNAL '45000' a ReglaNegocioException
// viven en RepositorioBase.
public sealed class RepositorioCita : RepositorioBase, IRepositorioCita
{
    public RepositorioCita(IConfiguration config)
        : base(config)
    {
    }

    // ---------- consultas ----------

    public Task<IReadOnlyList<CitaAgendaFila>> ConsultarAgendaAsync(
        DateTime desde, DateTime hasta, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Desde", desde);
        parametros.Add("p_Hasta", hasta);

        return ConsultarAsync<CitaAgendaFila>("SP_Cita_ConsultarAgenda_CRM", parametros, cancelacion);
    }

    public async Task<CitaResumenFila?> ObtenerAsync(int idCita, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdCita", idCita);

        var filas = await ConsultarAsync<CitaResumenFila>("SP_Cita_ObtenerDetalle", parametros, cancelacion);

        return filas.Count > 0 ? filas[0] : null;
    }

    public Task<IReadOnlyList<EstudianteBusquedaResponse>> BuscarEstudiantesAsync(
        string texto, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Texto", texto);

        return ConsultarAsync<EstudianteBusquedaResponse>("SP_Cita_BuscarEstudiantes_CRM", parametros, cancelacion);
    }

    public Task<IReadOnlyList<EspacioDisponibleResponse>> ConsultarDisponibilidadAsync(
        DateTime fecha, int idTipoSesion, int? idCitaExcluir, int? idGrupoExcluir, DateTime ahora,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Fecha", fecha.Date);
        parametros.Add("p_IdTipoSesion", idTipoSesion);
        parametros.Add("p_IdCitaExcluir", idCitaExcluir);
        parametros.Add("p_IdGrupoExcluir", idGrupoExcluir);
        parametros.Add("p_Ahora", ahora);

        return ConsultarAsync<EspacioDisponibleResponse>("SP_Cita_ConsultarDisponibilidad_CRM", parametros, cancelacion);
    }

    public Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion)
        => ConsultarAsync<TipoSesionResponse>("SP_ListarTiposSesion_CRM", null, cancelacion);

    public Task<IReadOnlyList<ModalidadResponse>> ListarModalidadesAsync(CancellationToken cancelacion)
        => ConsultarAsync<ModalidadResponse>("SP_ListarModalidades_CRM", null, cancelacion);

    // ---------- citas individuales ----------

    public Task<int> CrearAsync(
        int idUsuarioAccion, int idEstudiante, int idTipoSesion, int idModalidad,
        DateTime inicio, DateTime fin, string? observaciones, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdTipoSesion", idTipoSesion);
        parametros.Add("p_IdModalidad", idModalidad);
        parametros.Add("p_FechaHoraInicio", inicio);
        parametros.Add("p_FechaHoraFin", fin);
        parametros.Add("p_Observaciones", observaciones);

        return EscalarAsync("SP_Cita_Crear", parametros, cancelacion);
    }

    public Task ReprogramarAsync(
        int idUsuarioAccion, int idCita, DateTime inicio, DateTime fin, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdCita", idCita);
        parametros.Add("p_FechaHoraInicio", inicio);
        parametros.Add("p_FechaHoraFin", fin);

        return EjecutarAsync("SP_Cita_Reprogramar", parametros, cancelacion);
    }

    public Task CancelarAsync(int idUsuarioAccion, int idCita, string motivo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdCita", idCita);
        parametros.Add("p_Motivo", motivo);

        return EjecutarAsync("SP_Cita_Cancelar", parametros, cancelacion);
    }

    public Task MarcarNoAsistioAsync(int idUsuarioAccion, int idCita, DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdCita", idCita);
        parametros.Add("p_Ahora", ahora);

        return EjecutarAsync("SP_Cita_MarcarNoAsistio_CRM", parametros, cancelacion);
    }

    // La usa el servicio en segundo plano; devuelve cuantas citas se completaron
    public Task<int> CompletarVencidasAsync(DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Ahora", ahora);

        return EscalarAsync("SP_Cita_CompletarVencidas_CRM", parametros, cancelacion);
    }

    // ---------- grupos ----------

    public async Task<GrupoResumenFila?> ObtenerGrupoAsync(int idGrupoCita, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdGrupoCita", idGrupoCita);

        var filas = await ConsultarAsync<GrupoResumenFila>("SP_Grupo_Obtener_CRM", parametros, cancelacion);

        return filas.Count > 0 ? filas[0] : null;
    }

    public Task<IReadOnlyList<GrupoDisponibleResponse>> ConsultarGruposDisponiblesAsync(
        DateTime fecha, DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Fecha", fecha.Date);
        parametros.Add("p_Ahora", ahora);

        return ConsultarAsync<GrupoDisponibleResponse>("SP_Grupo_ConsultarDisponibles_CRM", parametros, cancelacion);
    }

    public Task<int> CrearGrupoAsync(
        int idUsuarioAccion, int idTipoSesion, int idModalidad, string? nombre,
        DateTime inicio, DateTime fin, int cupoMaximo, string idsEstudiantesJson, string? observaciones,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdTipoSesion", idTipoSesion);
        parametros.Add("p_IdModalidad", idModalidad);
        parametros.Add("p_Nombre", nombre);
        parametros.Add("p_Inicio", inicio);
        parametros.Add("p_Fin", fin);
        parametros.Add("p_CupoMaximo", cupoMaximo);
        parametros.Add("p_EstudiantesJson", idsEstudiantesJson);
        parametros.Add("p_Observaciones", observaciones);

        return EscalarAsync("SP_Grupo_Crear_CRM", parametros, cancelacion);
    }

    public Task<int> AgregarEstudianteGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, int idEstudiante, DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdGrupoCita", idGrupoCita);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Ahora", ahora);

        return EscalarAsync("SP_Grupo_AgregarEstudiante_CRM", parametros, cancelacion);
    }

    public Task ReprogramarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, DateTime inicio, DateTime fin, DateTime ahora,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdGrupoCita", idGrupoCita);
        parametros.Add("p_Inicio", inicio);
        parametros.Add("p_Fin", fin);
        parametros.Add("p_Ahora", ahora);

        return EjecutarAsync("SP_Grupo_Reprogramar_CRM", parametros, cancelacion);
    }

    public Task CancelarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, string motivo, DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdGrupoCita", idGrupoCita);
        parametros.Add("p_Motivo", motivo);
        parametros.Add("p_Ahora", ahora);

        return EjecutarAsync("SP_Grupo_Cancelar_CRM", parametros, cancelacion);
    }

    public Task EditarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, string? nombre, int cupoMaximo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdGrupoCita", idGrupoCita);
        parametros.Add("p_Nombre", nombre);
        parametros.Add("p_CupoMaximo", cupoMaximo);

        return EjecutarAsync("SP_Grupo_Editar_CRM", parametros, cancelacion);
    }
}