using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioConfiguracion
{
    Task<IReadOnlyList<FranjaHorarioResponse>> ListarHorarioAsync(CancellationToken cancelacion);

    Task<GuardarHorarioResponse> GuardarHorarioAsync(
        int idUsuarioAccion, GuardarHorarioRequest peticion, CancellationToken cancelacion);

    Task<IReadOnlyList<DiaNoLaboralResponse>> ListarDiasNoLaboralesAsync(CancellationToken cancelacion);

    Task<IReadOnlyList<DiaNoLaboralResponse>> ListarDiasNoLaboralesAsync(DateOnly desde, DateOnly hasta, CancellationToken cancelacion);


    Task<int> AgregarDiaNoLaboralAsync(
        int idUsuarioAccion, AgregarDiaNoLaboralRequest peticion, CancellationToken cancelacion);

    Task EliminarDiaNoLaboralAsync(int idUsuarioAccion, int idDiaNoLaboral, CancellationToken cancelacion);

    Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion);

    Task<int> CrearTipoSesionAsync(
        int idUsuarioAccion, GuardarTipoSesionRequest peticion, CancellationToken cancelacion);

    Task EditarTipoSesionAsync(
        int idUsuarioAccion, int idTipoSesion, GuardarTipoSesionRequest peticion, CancellationToken cancelacion);

    Task<DatosConsultorioResponse> ObtenerDatosConsultorioAsync(CancellationToken cancelacion);

    Task GuardarDatosConsultorioAsync(
        int idUsuarioAccion, GuardarDatosConsultorioRequest peticion, CancellationToken cancelacion);
}