using MenteActiva.Api.Infraestructura;
using Microsoft.Extensions.Options;

namespace MenteActiva.Api.Services;

// Cada N minutos programa los recordatorios de las citas proximas y manda lo que
// este pendiente. Molde: ServicioCompletarCitasVencidas, que hace lo mismo con
// las citas vencidas.
//
// Es singleton, como todo hosted service, y el servicio de notificaciones es
// scoped: por eso se crea un scope por vuelta.
//
// Las dos cosas van en el mismo worker y en este orden a proposito: si el
// recordatorio de una cita vence entre dos vueltas, programarlo justo antes de
// despachar hace que salga en la misma pasada y no en la siguiente.
public sealed class ServicioEnviarNotificaciones : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ServicioEnviarNotificaciones> _registro;
    private readonly TimeSpan _intervalo;

    public ServicioEnviarNotificaciones(
        IServiceScopeFactory scopes,
        IOptions<OpcionesNotificaciones> opciones,
        ILogger<ServicioEnviarNotificaciones> registro)
    {
        _scopes = scopes;
        _registro = registro;
        _intervalo = TimeSpan.FromMinutes(opciones.Value.MinutosRevision);
    }

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        using var temporizador = new PeriodicTimer(_intervalo);

        try
        {
            // Una vuelta al arrancar y luego cada intervalo
            do
            {
                await VueltaAsync(detener);
            }
            while (await temporizador.WaitForNextTickAsync(detener));
        }
        catch (OperationCanceledException) when (detener.IsCancellationRequested)
        {
            // El API se esta apagando: salida normal
        }
    }

    private async Task VueltaAsync(CancellationToken detener)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IServicioNotificaciones>();

            var programados = await servicio.ProgramarRecordatoriosAsync(detener);

            if (programados > 0)
            {
                _registro.LogInformation("Se programaron {Cantidad} recordatorios.", programados);
            }

            var (enviados, fallidos) = await servicio.DespacharPendientesAsync(detener);

            if (enviados > 0 || fallidos > 0)
            {
                _registro.LogInformation(
                    "Avisos: {Enviados} enviados, {Fallidos} fallidos.", enviados, fallidos);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un fallo puntual (base caida, SMTP caido) no debe matar el
            // servicio: se reintenta en la proxima vuelta
            _registro.LogError(ex, "No se pudo completar la vuelta de notificaciones.");
        }
    }
}
