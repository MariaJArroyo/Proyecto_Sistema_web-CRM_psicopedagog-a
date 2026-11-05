using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Mensajeria interna (HU-M10-4). Aparte del repositorio de comunicacion porque
// no comparten ni una tabla: aquello es TB_NOTIFICACION y esto es TB_MENSAJE.
//
// Todo va con el id del usuario que pregunta y los procedimientos validan que
// participe del hilo. Asi, cambiando un id en la URL no se lee la conversacion
// de otra familia.
public interface IRepositorioMensaje
{
    Task<IReadOnlyList<HiloResponse>> ListarHilosAsync(int idUsuario, CancellationToken cancelacion);

    // Marca como leidos los que iban para este usuario y devuelve la conversacion
    Task<IReadOnlyList<MensajeResponse>> AbrirHiloAsync(
        int idUsuario, int idHilo, CancellationToken cancelacion);

    Task<IReadOnlyList<DestinatarioMensajeResponse>> ListarDestinatariosAsync(
        int idUsuario, CancellationToken cancelacion);

    Task<int> ContarSinLeerAsync(int idUsuario, CancellationToken cancelacion);

    // Devuelve el id del mensaje y el del hilo al que quedo colgado
    Task<(int IdMensaje, int IdHilo)> EnviarAsync(
        int idUsuarioRemitente, int? idUsuarioDestinatario, int? idHilo,
        string? asunto, string cuerpo, CancellationToken cancelacion);
}
