using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioCita
{
    // ---------- citas ----------

    Task<IReadOnlyList<CitaAgendaFila>> ConsultarAgendaAsync(DateTime desde, DateTime hasta, CancellationToken cancelacion);

    Task<CitaResumenFila?> ObtenerAsync(int idCita, CancellationToken cancelacion);

    Task<IReadOnlyList<EstudianteBusquedaResponse>> BuscarEstudiantesAsync(string texto, CancellationToken cancelacion);

    Task<IReadOnlyList<EspacioDisponibleResponse>> ConsultarDisponibilidadAsync(
        DateTime fecha, int idTipoSesion, int? idCitaExcluir, int? idGrupoExcluir, DateTime ahora,
        CancellationToken cancelacion);

    Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion);

    Task<IReadOnlyList<ModalidadResponse>> ListarModalidadesAsync(CancellationToken cancelacion);

    Task<int> CrearAsync(
        int idUsuarioAccion, int idEstudiante, int idTipoSesion, int idModalidad,
        DateTime inicio, DateTime fin, string? observaciones, CancellationToken cancelacion);

    Task ReprogramarAsync(int idUsuarioAccion, int idCita, DateTime inicio, DateTime fin, CancellationToken cancelacion);

    Task CancelarAsync(int idUsuarioAccion, int idCita, string motivo, CancellationToken cancelacion);

    Task MarcarNoAsistioAsync(int idUsuarioAccion, int idCita, DateTime ahora, CancellationToken cancelacion);

    Task<int> CompletarVencidasAsync(DateTime ahora, CancellationToken cancelacion);

    // ---------- grupos ----------

    Task<GrupoResumenFila?> ObtenerGrupoAsync(int idGrupoCita, CancellationToken cancelacion);

    Task<IReadOnlyList<GrupoDisponibleResponse>> ConsultarGruposDisponiblesAsync(
        DateTime fecha, DateTime ahora, CancellationToken cancelacion);

    Task<int> CrearGrupoAsync(
        int idUsuarioAccion, int idTipoSesion, int idModalidad, string? nombre,
        DateTime inicio, DateTime fin, int cupoMaximo, string idsEstudiantesJson, string? observaciones,
        CancellationToken cancelacion);

    Task<int> AgregarEstudianteGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, int idEstudiante, DateTime ahora, CancellationToken cancelacion);

    Task ReprogramarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, DateTime inicio, DateTime fin, DateTime ahora,
        CancellationToken cancelacion);

    Task CancelarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, string motivo, DateTime ahora, CancellationToken cancelacion);

    Task EditarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, string? nombre, int cupoMaximo, CancellationToken cancelacion);
}