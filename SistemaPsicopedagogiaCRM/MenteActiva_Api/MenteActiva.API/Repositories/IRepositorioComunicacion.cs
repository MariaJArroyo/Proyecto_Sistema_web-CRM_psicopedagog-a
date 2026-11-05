using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioComunicacion
{
    // Los dos resultados de SP_Correo_ListarDestinatarios ya cruzados
    Task<IReadOnlyList<DestinatarioCorreoResponse>> ListarDestinatariosAsync(
        CancellationToken cancelacion);

    // Las citas del estudiante que se pueden mencionar en un correo: es lo que
    // resuelve [Fecha], [Hora] y [Motivo].
    Task<IReadOnlyList<CitaCorreoResponse>> ListarCitasEstudianteAsync(
        int idEstudiante, CancellationToken cancelacion);

    // Historial. soloManuales deja fuera lo que genero el sistema.
    Task<IReadOnlyList<NotificacionResponse>> ListarNotificacionesAsync(
        bool soloManuales, int? limite, CancellationToken cancelacion);

    // Deja la notificacion pendiente y devuelve su id. El envio va despues.
    // idCita queda anotado cuando el correo hablaba de una cita.
    Task<int> RegistrarCorreoAsync(
        int idUsuarioRegistro, int idEncargado, int? idCita, string correoDestino,
        string asunto, string cuerpo, CancellationToken cancelacion);

    Task MarcarEnviadaAsync(int idNotificacion, CancellationToken cancelacion);

    Task MarcarFallidaAsync(int idNotificacion, string mensajeError, CancellationToken cancelacion);

    // ---------- avisos automaticos (HU-M10-2) ----------

    // Deja programados los recordatorios de las citas que se acercan y devuelve
    // cuantos creo. Es reejecutable: no duplica.
    Task<int> ProgramarRecordatoriosAsync(CancellationToken cancelacion);

    // Lo que toca despachar ahora: pendientes de canal Correo cuya hora ya llego
    // y que no pasaron el tope de intentos.
    Task<IReadOnlyList<NotificacionPendienteResponse>> ObtenerPendientesAsync(
        int maxIntentos, int limite, CancellationToken cancelacion);
}
