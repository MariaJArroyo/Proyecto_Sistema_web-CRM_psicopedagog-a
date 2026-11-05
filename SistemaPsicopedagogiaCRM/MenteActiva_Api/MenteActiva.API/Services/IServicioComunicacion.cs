using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioComunicacion
{
    Task<CatalogosCorreoResponse> ObtenerCatalogosAsync(CancellationToken cancelacion);

    Task<IReadOnlyList<NotificacionResponse>> ListarNotificacionesAsync(
        bool soloManuales, int? limite, CancellationToken cancelacion);

    // Las citas del estudiante, para que la pantalla pueda ofrecer de cual habla
    // el correo y con eso resolver [Fecha], [Hora] y [Motivo].
    Task<IReadOnlyList<CitaCorreoResponse>> ListarCitasEstudianteAsync(
        int idEstudiante, CancellationToken cancelacion);

    // El mismo texto que saldria, sin mandarlo. Dice tambien que marcadores
    // quedaron sin resolver, que son los que bloquean el envio.
    Task<VistaPreviaCorreoResponse> VistaPreviaAsync(
        EnviarCorreoRequest peticion, CancellationToken cancelacion);

    Task<CorreoEnviadoResponse> EnviarCorreoAsync(
        int idUsuarioAccion, EnviarCorreoRequest peticion, CancellationToken cancelacion);
}
