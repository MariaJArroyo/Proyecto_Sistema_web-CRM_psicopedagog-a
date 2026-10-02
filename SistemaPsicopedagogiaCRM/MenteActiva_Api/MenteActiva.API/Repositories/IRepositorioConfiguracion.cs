using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioConfiguracion
{
    Task<IReadOnlyList<FranjaHorarioFila>> ListarHorarioAsync(CancellationToken cancelacion);

    Task<IReadOnlyList<CitaFueraDeHorarioResponse>> GuardarHorarioAsync(
        int idUsuarioAccion, string franjasJson, DateTime ahora, CancellationToken cancelacion);

    Task<IReadOnlyList<DiaNoLaboralFila>> ListarDiasNoLaboralesAsync(DateTime desde, CancellationToken cancelacion);

    Task<int> AgregarDiaNoLaboralAsync(
        int idUsuarioAccion, DateTime fecha, string motivo, bool esFeriado, DateTime hoy,
        CancellationToken cancelacion);

    Task EliminarDiaNoLaboralAsync(int idUsuarioAccion, int idDiaNoLaboral, CancellationToken cancelacion);

    Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion);

    Task<int> GuardarTipoSesionAsync(
        int idUsuarioAccion, int? idTipoSesion, string nombre, int duracionMinutos,
        CancellationToken cancelacion);

    Task<IReadOnlyList<ConfiguracionFila>> ListarConfiguracionAsync(CancellationToken cancelacion);

    Task GuardarConfiguracionAsync(int idUsuarioAccion, string valoresJson, CancellationToken cancelacion);
}