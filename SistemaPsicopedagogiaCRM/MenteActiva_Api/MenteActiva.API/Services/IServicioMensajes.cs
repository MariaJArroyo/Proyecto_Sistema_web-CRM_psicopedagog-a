using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioMensajes
{
    Task<IReadOnlyList<HiloResponse>> ListarHilosAsync(int idUsuario, CancellationToken cancelacion);

    Task<IReadOnlyList<MensajeResponse>> AbrirHiloAsync(
        int idUsuario, int idHilo, CancellationToken cancelacion);

    Task<IReadOnlyList<DestinatarioMensajeResponse>> ListarDestinatariosAsync(
        int idUsuario, CancellationToken cancelacion);

    Task<int> ContarSinLeerAsync(int idUsuario, CancellationToken cancelacion);

    // esEncargado sale del token: decide si el hilo nuevo pide destinatario
    Task<MensajeEnviadoResponse> EnviarAsync(
        int idUsuario, bool esEncargado, EnviarMensajeRequest peticion, CancellationToken cancelacion);

    Task<bool> TieneAccesoAsync(int idUsuario, CancellationToken cancelacion);

    Task<IReadOnlyList<AccesoMensajeriaRolResponse>> ListarAccesoAsync(CancellationToken cancelacion);

    Task GuardarAccesoAsync(
        int idUsuarioAccion, GuardarAccesoMensajeriaRequest peticion, CancellationToken cancelacion);
}
