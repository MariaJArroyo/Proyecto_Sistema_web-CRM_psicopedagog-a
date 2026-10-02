using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioCitas
{
    // ---------- citas ----------

    Task<IReadOnlyList<CitaAgendaResponse>> ConsultarAgendaAsync(DateOnly desde, DateOnly hasta, CancellationToken cancelacion);

    Task<IReadOnlyList<EstudianteBusquedaResponse>> BuscarEstudiantesAsync(string? texto, CancellationToken cancelacion);

    Task<IReadOnlyList<EspacioDisponibleResponse>> ConsultarDisponibilidadAsync(
        DateOnly fecha, int idTipoSesion, int? idCitaExcluir, int? idGrupoExcluir, CancellationToken cancelacion);

    Task<CatalogosAgendaResponse> ObtenerCatalogosAsync(CancellationToken cancelacion);

    Task<int> CrearAsync(int idUsuarioAccion, CrearCitaRequest peticion, CancellationToken cancelacion);

    Task ReprogramarAsync(int idUsuarioAccion, int idCita, ReprogramarCitaRequest peticion, CancellationToken cancelacion);

    Task CancelarAsync(int idUsuarioAccion, int idCita, CancelarCitaRequest peticion, CancellationToken cancelacion);

    Task MarcarNoAsistioAsync(int idUsuarioAccion, int idCita, CancellationToken cancelacion);

    Task<int> CompletarVencidasAsync(CancellationToken cancelacion);

    // ---------- grupos ----------

    Task<IReadOnlyList<GrupoDisponibleResponse>> ConsultarGruposDisponiblesAsync(DateOnly fecha, CancellationToken cancelacion);

    Task<int> CrearGrupoAsync(int idUsuarioAccion, CrearGrupoRequest peticion, CancellationToken cancelacion);

    Task<int> AgregarEstudianteGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, AgregarEstudianteGrupoRequest peticion, CancellationToken cancelacion);

    Task ReprogramarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, ReprogramarCitaRequest peticion, CancellationToken cancelacion);

    Task CancelarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, CancelarCitaRequest peticion, CancellationToken cancelacion);

    Task EditarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, EditarGrupoRequest peticion, CancellationToken cancelacion);
}