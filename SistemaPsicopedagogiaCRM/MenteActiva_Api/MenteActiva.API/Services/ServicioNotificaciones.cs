using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Repositories;
using Microsoft.Extensions.Options;

namespace MenteActiva.Api.Services;

// Programa los recordatorios y despacha lo pendiente. El texto ya viene armado
// desde la base: aqui no se sustituye nada, porque el aviso de cita se arma
// dentro de la transaccion de la agenda con FN_Plantilla_Sustituir.
public sealed class ServicioNotificaciones : IServicioNotificaciones
{
    private readonly IRepositorioComunicacion _repositorio;
    private readonly IServicioConfiguracion _configuracion;
    private readonly IServicioCorreo _correo;
    private readonly OpcionesNotificaciones _opciones;
    private readonly ILogger<ServicioNotificaciones> _registro;

    public ServicioNotificaciones(
        IRepositorioComunicacion repositorio,
        IServicioConfiguracion configuracion,
        IServicioCorreo correo,
        IOptions<OpcionesNotificaciones> opciones,
        ILogger<ServicioNotificaciones> registro)
    {
        _repositorio = repositorio;
        _configuracion = configuracion;
        _correo = correo;
        _opciones = opciones.Value;
        _registro = registro;
    }

    public Task<int> ProgramarRecordatoriosAsync(CancellationToken cancelacion)
        => _repositorio.ProgramarRecordatoriosAsync(cancelacion);

    public async Task<(int Enviados, int Fallidos)> DespacharPendientesAsync(
        CancellationToken cancelacion)
    {
        var pendientes = await _repositorio.ObtenerPendientesAsync(
            _opciones.MaxIntentos, _opciones.PorVuelta, cancelacion);

        if (pendientes.Count == 0)
        {
            return (0, 0);
        }

        var datos = await _configuracion.ObtenerDatosConsultorioAsync(cancelacion);

        var enviados = 0;
        var fallidos = 0;

        foreach (var pendiente in pendientes)
        {
            cancelacion.ThrowIfCancellationRequested();

            var asunto = string.IsNullOrWhiteSpace(pendiente.Asunto)
                ? datos.NombreConsultorio
                : pendiente.Asunto;

            try
            {
                var html = MensajeComunicacion.Armar(asunto, pendiente.Cuerpo, datos.NombreConsultorio);

                await _correo.EnviarAsync(pendiente.Correo, asunto, html);
                await _repositorio.MarcarEnviadaAsync(pendiente.IdNotificacion, cancelacion);

                enviados++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Un aviso que falla no detiene a los demas: se marca y se sigue.
                // Al llegar al tope de intentos deja de reintentarse solo, porque
                // el procedimiento ya no lo devuelve.
                _registro.LogError(ex,
                    "No se pudo enviar la notificacion {IdNotificacion} a {Correo}.",
                    pendiente.IdNotificacion, pendiente.Correo);

                await _repositorio.MarcarFallidaAsync(
                    pendiente.IdNotificacion, ex.Message, cancelacion);

                fallidos++;
            }
        }

        return (enviados, fallidos);
    }
}
