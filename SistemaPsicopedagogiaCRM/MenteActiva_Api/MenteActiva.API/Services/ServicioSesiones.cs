using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Lo que depende de "ahora" lo sabe el API (hora de Costa Rica), no la base:
// por eso el reloj se inyecta y se le pasa a las SPs. El traslape, la fecha
// futura y el estado de la cita los valida el procedimiento, que es el unico
// lugar por donde se escribe.
public sealed class ServicioSesiones : IServicioSesiones
{
    private const int MinimoLetrasBusqueda = 2;
    private const int MaximoLetrasBusqueda = 100;
    private const int MaximoDiasConsulta = 366;

    private readonly IRepositorioSesion _repositorio;
    private readonly IRelojNegocio _reloj;

    public ServicioSesiones(IRepositorioSesion repositorio, IRelojNegocio reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    public Task<IReadOnlyList<SesionListaResponse>> ListarAsync(
        int? idEstudiante, DateOnly? desde, DateOnly? hasta, CancellationToken cancelacion)
    {
        if (desde is not null && hasta is not null)
        {
            if (hasta < desde)
            {
                throw new ReglaNegocioException("El rango de fechas no es válido.");
            }

            if (desde.Value.DayNumber + MaximoDiasConsulta < hasta.Value.DayNumber)
            {
                throw new ReglaNegocioException("El rango no puede pasar de un año.");
            }
        }

        if (idEstudiante is <= 0)
        {
            throw new ReglaNegocioException("El estudiante indicado no es válido.");
        }

        return _repositorio.ListarAsync(idEstudiante, desde, hasta, cancelacion);
    }

    public async Task<SesionDetalleResponse> ObtenerAsync(int idSesion, CancellationToken cancelacion)
    {
        var sesion = await _repositorio.ObtenerAsync(idSesion, cancelacion);

        // 404 lo decide el controlador; aqui solo se dice que no esta
        return sesion ?? throw new ReglaNegocioException("La sesión indicada no existe.");
    }

    public Task<IReadOnlyList<CitaDisponibleSesionResponse>> ListarCitasDisponiblesAsync(
        int idEstudiante, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException("Seleccione un estudiante.");
        }

        return _repositorio.ListarCitasDisponiblesAsync(idEstudiante, _reloj.Ahora, cancelacion);
    }

    public async Task<IReadOnlyList<EstudianteBusquedaResponse>> BuscarEstudiantesAsync(
        string? texto, CancellationToken cancelacion)
    {
        var limpio = texto?.Trim() ?? string.Empty;

        // Con una letra o con un texto larguisimo no se va a la base: la primera
        // traeria casi todo y la segunda no puede calzar con nada
        if (limpio.Length < MinimoLetrasBusqueda || limpio.Length > MaximoLetrasBusqueda)
        {
            return [];
        }

        return await _repositorio.BuscarEstudiantesAsync(limpio, cancelacion);
    }

    public async Task<CatalogosSesionResponse> ObtenerCatalogosAsync(CancellationToken cancelacion)
    {
        var tiposSesion = await _repositorio.ListarTiposSesionAsync(cancelacion);
        var tiposAtencion = await _repositorio.ListarTiposAtencionAsync(cancelacion);

        return new CatalogosSesionResponse
        {
            TiposSesion = tiposSesion,
            TiposAtencion = tiposAtencion
        };
    }

    // =====================================================
    // ESCRITURAS
    // =====================================================

    public Task<int> RegistrarAsync(
        int idUsuarioAccion, RegistrarSesionRequest peticion, CancellationToken cancelacion)
    {
        // IdCita llega como 0 cuando el select del formulario viene vacio
        var saneada = peticion with { IdCita = peticion.IdCita is > 0 ? peticion.IdCita : null };

        return _repositorio.RegistrarAsync(idUsuarioAccion, saneada, _reloj.Ahora, cancelacion);
    }

    public Task EditarAsync(
        int idUsuarioAccion, int idSesion, EditarSesionRequest peticion, CancellationToken cancelacion)
    {
        if (idSesion <= 0)
        {
            throw new ReglaNegocioException("La sesión indicada no existe.");
        }

        return _repositorio.EditarAsync(idUsuarioAccion, idSesion, peticion, _reloj.Ahora, cancelacion);
    }

    public Task InactivarAsync(int idUsuarioAccion, int idSesion, CancellationToken cancelacion)
    {
        if (idSesion <= 0)
        {
            throw new ReglaNegocioException("La sesión indicada no existe.");
        }

        return _repositorio.InactivarAsync(idUsuarioAccion, idSesion, _reloj.Ahora, cancelacion);
    }
}
