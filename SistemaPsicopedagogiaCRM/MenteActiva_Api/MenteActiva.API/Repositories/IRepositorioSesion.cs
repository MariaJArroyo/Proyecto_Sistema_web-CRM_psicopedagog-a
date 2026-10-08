using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioSesion
{
    // ---------- consultas ----------

    Task<IReadOnlyList<SesionListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde, DateOnly? hasta, CancellationToken cancelacion);

    Task<SesionDetalleResponse?> ObtenerAsync(int idSesion, CancellationToken cancelacion);

    Task<IReadOnlyList<CitaDisponibleSesionResponse>> ListarCitasDisponiblesAsync(
        int idEstudiante, DateTime ahora, CancellationToken cancelacion);

    // El buscador se reutiliza de la agenda: SP_Cita_BuscarEstudiantes_CRM hace
    // exactamente esto y escribir otro seria duplicarlo
    Task<IReadOnlyList<EstudianteBusquedaResponse>> BuscarEstudiantesAsync(
        string texto, CancellationToken cancelacion);

    Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion);

    Task<IReadOnlyList<TipoAtencionResponse>> ListarTiposAtencionAsync(CancellationToken cancelacion);

    // ---------- escrituras ----------

    Task<int> RegistrarAsync(
        int idUsuarioAccion, RegistrarSesionRequest peticion, DateTime ahora, CancellationToken cancelacion);

    Task EditarAsync(
        int idUsuarioAccion, int idSesion, EditarSesionRequest peticion, DateTime ahora,
        CancellationToken cancelacion);

    Task InactivarAsync(
        int idUsuarioAccion, int idSesion, DateTime ahora, CancellationToken cancelacion);
}
