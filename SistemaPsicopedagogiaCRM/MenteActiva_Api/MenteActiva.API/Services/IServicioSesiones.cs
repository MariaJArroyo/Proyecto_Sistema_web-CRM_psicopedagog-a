using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioSesiones
{
    // ---------- consultas ----------

    Task<IReadOnlyList<SesionListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde, DateOnly? hasta, CancellationToken cancelacion);

    Task<SesionDetalleResponse> ObtenerAsync(int idSesion, CancellationToken cancelacion);

    Task<IReadOnlyList<CitaDisponibleSesionResponse>> ListarCitasDisponiblesAsync(
        int idEstudiante, CancellationToken cancelacion);

    Task<IReadOnlyList<EstudianteBusquedaResponse>> BuscarEstudiantesAsync(
        string? texto, CancellationToken cancelacion);

    Task<CatalogosSesionResponse> ObtenerCatalogosAsync(CancellationToken cancelacion);

    // ---------- escrituras ----------

    Task<int> RegistrarAsync(
        int idUsuarioAccion, RegistrarSesionRequest peticion, CancellationToken cancelacion);

    Task EditarAsync(
        int idUsuarioAccion, int idSesion, EditarSesionRequest peticion, CancellationToken cancelacion);

    Task InactivarAsync(int idUsuarioAccion, int idSesion, CancellationToken cancelacion);
}
