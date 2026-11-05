namespace MenteActiva.Api.Services;

// Lo que hace el worker en cada vuelta, separado del envio manual de correo
// porque son dos cosas distintas: una la dispara una persona y la otra el reloj.
public interface IServicioNotificaciones
{
    // Deja programados los recordatorios de las citas proximas. Devuelve cuantos
    // creo; es reejecutable y no duplica.
    Task<int> ProgramarRecordatoriosAsync(CancellationToken cancelacion);

    // Manda lo que este pendiente y marca cada uno enviado o fallido.
    // Devuelve cuantos salieron y cuantos fallaron.
    Task<(int Enviados, int Fallidos)> DespacharPendientesAsync(CancellationToken cancelacion);
}
