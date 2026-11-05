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

    Task<MensajeEnviadoResponse> EnviarAsync(
        int idUsuario, EnviarMensajeRequest peticion, CancellationToken cancelacion);
}
