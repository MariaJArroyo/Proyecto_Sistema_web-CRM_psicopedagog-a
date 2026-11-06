using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Repositories;
using Microsoft.Extensions.Options;

namespace MenteActiva.Api.Services;

// Esta capa pone las dos cosas que el repositorio no deberia adivinar: el tope
// de intentos, que sale de la misma configuracion del worker, y la hora del
// consultorio, que sale del reloj de negocio y no de la maquina.
public sealed class ServicioContadores : IServicioContadores
{
    private readonly IRepositorioContadores _repositorio;
    private readonly IRelojNegocio _reloj;
    private readonly OpcionesNotificaciones _opciones;

    public ServicioContadores(
        IRepositorioContadores repositorio,
        IRelojNegocio reloj,
        IOptions<OpcionesNotificaciones> opciones)
    {
        _repositorio = repositorio;
        _reloj = reloj;
        _opciones = opciones.Value;
    }

    public Task<int> EncargadosSinInvitarAsync(CancellationToken cancelacion)
        => _repositorio.EncargadosSinInvitarAsync(cancelacion);

    // El mismo MaxIntentos con el que el worker deja de reintentar, para que la
    // burbuja no cuente como fallido algo que todavia se va a volver a mandar
    public Task<int> NotificacionesFallidasAsync(CancellationToken cancelacion)
        => _repositorio.NotificacionesFallidasAsync(_opciones.MaxIntentos, cancelacion);

    public Task<int> CitasSinSesionAsync(CancellationToken cancelacion)
        => _repositorio.CitasSinSesionAsync(_reloj.Ahora, cancelacion);

    public Task<int> PagosVencidosAsync(CancellationToken cancelacion)
        => _repositorio.PagosVencidosAsync(cancelacion);
}
