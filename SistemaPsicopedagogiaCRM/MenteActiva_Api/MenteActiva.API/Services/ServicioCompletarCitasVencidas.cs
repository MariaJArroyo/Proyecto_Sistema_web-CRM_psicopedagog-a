using MenteActiva.Api.Infraestructura;
using Microsoft.Extensions.Options;

namespace MenteActiva.Api.Services;

// Cada N minutos pasa a "Completada" las citas cuya hora ya termino, para que
// el Dashboard y el portal lo reflejen aunque nadie abra la agenda.
// Es singleton (como todo hosted service): el servicio de citas es scoped, por
// eso se crea un scope por vuelta.
public sealed class ServicioCompletarCitasVencidas : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ServicioCompletarCitasVencidas> _registro;
    private readonly TimeSpan _intervalo;

    public ServicioCompletarCitasVencidas(
        IServiceScopeFactory scopes,
        IOptions<OpcionesAgenda> opciones,
        ILogger<ServicioCompletarCitasVencidas> registro)
    {
        _scopes = scopes;
        _registro = registro;
        _intervalo = TimeSpan.FromMinutes(opciones.Value.MinutosRevisionVencidas);
    }

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        using var temporizador = new PeriodicTimer(_intervalo);

        try
        {
            // Una vuelta al arrancar y luego cada intervalo
            do
            {
                await CompletarAsync(detener);
            }
            while (await temporizador.WaitForNextTickAsync(detener));
        }
        catch (OperationCanceledException) when (detener.IsCancellationRequested)
        {
            // El API se esta apagando: salida normal
        }
    }

    private async Task CompletarAsync(CancellationToken detener)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IServicioCitas>();

            var completadas = await servicio.CompletarVencidasAsync(detener);

            if (completadas > 0)
            {
                _registro.LogInformation("Se marcaron {Cantidad} citas como completadas.", completadas);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un fallo puntual (base caida) no debe matar el servicio: se reintenta en la proxima vuelta
            _registro.LogError(ex, "No se pudieron completar las citas vencidas.");
        }
    }
}