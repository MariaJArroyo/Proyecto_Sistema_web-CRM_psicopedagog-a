using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Mensajeria interna (HU-M10-4). Aparte del repositorio de comunicacion porque
// no comparten ni una tabla: aquello es TB_NOTIFICACION y esto es TB_MENSAJE.
//
// Todo va con el id del usuario que pregunta y los procedimientos validan que
// pueda ver el hilo: el encargado solo los de su familia y el personal solo si
// su rol tiene acceso a la bandeja del consultorio. Asi, cambiando un id en la
// URL no se lee la conversacion de otra familia.
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

    // Si el usuario ve la bandeja del consultorio (FN_Mensajeria_TieneAcceso)
    Task<bool> TieneAccesoAsync(int idUsuario, CancellationToken cancelacion);

    // Los tres roles del personal con su marca de acceso
    Task<IReadOnlyList<AccesoMensajeriaRolResponse>> ListarAccesoAsync(CancellationToken cancelacion);

    // rolesJson es la lista completa de roles que quedan con acceso, ej. "[2,3]"
    Task GuardarAccesoAsync(int idUsuarioAccion, string rolesJson, CancellationToken cancelacion);
}
