using System.Text.Json;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Reglas de la agenda que dependen de "ahora": eso lo sabe el API (hora de
// Costa Rica), no la base. Lo demas (horario de atencion, traslapes, cupos,
// dias no laborales) ya lo validan las SPs.
public sealed class ServicioCitas : IServicioCitas
{
    private const int EstadoProgramada = 1;
    private const int EstadoConfirmada = 2;
    private const int EstadoCompletada = 3;

    private const int MaximoDiasConsulta = 62;
    private const int MinimoLetrasBusqueda = 2;
    private const int MaximoLetrasBusqueda = 100;

    private readonly IRepositorioCita _repositorio;
    private readonly IRelojNegocio _reloj;

    public ServicioCitas(IRepositorioCita repositorio, IRelojNegocio reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    // =====================================================
    // CITAS
    // =====================================================

    public async Task<IReadOnlyList<CitaAgendaResponse>> ConsultarAgendaAsync(
        DateOnly desde, DateOnly hasta, CancellationToken cancelacion)
    {
        if (hasta <= desde)
        {
            throw new ReglaNegocioException("El rango de fechas no es válido.");
        }

        if (hasta.DayNumber - desde.DayNumber > MaximoDiasConsulta)
        {
            throw new ReglaNegocioException($"Se pueden consultar como máximo {MaximoDiasConsulta} días.");
        }

        var ahora = _reloj.Ahora;

        // Antes de mostrar se completan las vencidas: la agenda nunca ensena como
        // pendiente una cita cuya hora ya paso, aunque el servicio de fondo no haya corrido
        await _repositorio.CompletarVencidasAsync(ahora, cancelacion);

        var filas = await _repositorio.ConsultarAgendaAsync(
            desde.ToDateTime(TimeOnly.MinValue),
            hasta.ToDateTime(TimeOnly.MinValue),
            cancelacion);

        var citas = filas.Select(fila => Describir(fila, ahora)).ToList();

        return MarcarGruposGestionables(citas);
    }

    public async Task<IReadOnlyList<EstudianteBusquedaResponse>> BuscarEstudiantesAsync(
        string? texto, CancellationToken cancelacion)
    {
        var limpio = texto?.Trim() ?? string.Empty;

        if (limpio.Length < MinimoLetrasBusqueda)
        {
            return [];
        }

        if (limpio.Length > MaximoLetrasBusqueda)
        {
            limpio = limpio[..MaximoLetrasBusqueda];
        }

        return await _repositorio.BuscarEstudiantesAsync(limpio, cancelacion);
    }

    public async Task<IReadOnlyList<EspacioDisponibleResponse>> ConsultarDisponibilidadAsync(
        DateOnly fecha, int idTipoSesion, int? idCitaExcluir, int? idGrupoExcluir, CancellationToken cancelacion)
    {
        if (fecha < _reloj.Hoy || idTipoSesion <= 0)
        {
            return [];
        }

        return await _repositorio.ConsultarDisponibilidadAsync(
            fecha.ToDateTime(TimeOnly.MinValue), idTipoSesion, idCitaExcluir, idGrupoExcluir,
            _reloj.Ahora, cancelacion);
    }

    public async Task<CatalogosAgendaResponse> ObtenerCatalogosAsync(CancellationToken cancelacion)
    {
        var tipos = _repositorio.ListarTiposSesionAsync(cancelacion);
        var modalidades = _repositorio.ListarModalidadesAsync(cancelacion);

        await Task.WhenAll(tipos, modalidades);

        return new CatalogosAgendaResponse(await tipos, await modalidades);
    }

    public async Task<int> CrearAsync(
        int idUsuarioAccion, CrearCitaRequest peticion, CancellationToken cancelacion)
    {
        var inicio = SinSegundos(peticion.FechaHoraInicio!.Value);

        ValidarQueSeaFutura(inicio, "No se puede agendar una cita en una hora que ya pasó.");

        var duracion = await ObtenerDuracionAsync(peticion.IdTipoSesion, cancelacion);

        return await _repositorio.CrearAsync(
            idUsuarioAccion,
            peticion.IdEstudiante,
            peticion.IdTipoSesion,
            peticion.IdModalidad,
            inicio,
            inicio.AddMinutes(duracion),
            Normalizar(peticion.Observaciones),
            cancelacion);
    }

    public async Task ReprogramarAsync(
        int idUsuarioAccion, int idCita, ReprogramarCitaRequest peticion, CancellationToken cancelacion)
    {
        var cita = await ObtenerCitaAsync(idCita, cancelacion);

        ValidarQueSeaFutura(cita.FechaHoraInicio, "Una cita que ya pasó no se puede reprogramar.");

        var inicio = SinSegundos(peticion.FechaHoraInicio!.Value);

        ValidarQueSeaFutura(inicio, "La nueva hora tiene que ser posterior a la actual.");

        // La duracion es la del tipo de sesion de la cita: al reprogramar solo cambia la hora
        var duracion = await ObtenerDuracionAsync(cita.IdTipoSesion, cancelacion);

        // Si la cita es de un grupo, la SP lo rechaza: se reprograma el grupo completo
        await _repositorio.ReprogramarAsync(
            idUsuarioAccion, idCita, inicio, inicio.AddMinutes(duracion), cancelacion);
    }

    public async Task CancelarAsync(
        int idUsuarioAccion, int idCita, CancelarCitaRequest peticion, CancellationToken cancelacion)
    {
        var cita = await ObtenerCitaAsync(idCita, cancelacion);

        ValidarQueSeaFutura(cita.FechaHoraInicio,
            "Una cita que ya pasó no se cancela. Si el estudiante no llegó, márquela como no asistió.");

        // En un grupo, cancela solo a este estudiante y libera su cupo
        await _repositorio.CancelarAsync(idUsuarioAccion, idCita, peticion.Motivo.Trim(), cancelacion);
    }

    public Task MarcarNoAsistioAsync(int idUsuarioAccion, int idCita, CancellationToken cancelacion)
        => _repositorio.MarcarNoAsistioAsync(idUsuarioAccion, idCita, _reloj.Ahora, cancelacion);

    public Task<int> CompletarVencidasAsync(CancellationToken cancelacion)
        => _repositorio.CompletarVencidasAsync(_reloj.Ahora, cancelacion);

    // =====================================================
    // GRUPOS
    // =====================================================

    public async Task<IReadOnlyList<GrupoDisponibleResponse>> ConsultarGruposDisponiblesAsync(
        DateOnly fecha, CancellationToken cancelacion)
    {
        if (fecha < _reloj.Hoy)
        {
            return [];
        }

        return await _repositorio.ConsultarGruposDisponiblesAsync(
            fecha.ToDateTime(TimeOnly.MinValue), _reloj.Ahora, cancelacion);
    }

    public async Task<int> CrearGrupoAsync(
        int idUsuarioAccion, CrearGrupoRequest peticion, CancellationToken cancelacion)
    {
        var ids = peticion.IdsEstudiantes.Distinct().ToList();

        // Mensajes claros antes de ir a la base (la SP valida lo mismo por si acaso)
        if (ids.Count != peticion.IdsEstudiantes.Count)
        {
            throw new ReglaNegocioException("Hay estudiantes repetidos en el grupo.");
        }

        if (ids.Count > peticion.CupoMaximo)
        {
            throw new ReglaNegocioException("Hay más estudiantes que cupos en el grupo.");
        }

        var inicio = SinSegundos(peticion.FechaHoraInicio!.Value);

        ValidarQueSeaFutura(inicio, "No se puede agendar un grupo en una hora que ya pasó.");

        var duracion = await ObtenerDuracionAsync(peticion.IdTipoSesion, cancelacion);

        return await _repositorio.CrearGrupoAsync(
            idUsuarioAccion,
            peticion.IdTipoSesion,
            peticion.IdModalidad,
            Normalizar(peticion.Nombre),
            inicio,
            inicio.AddMinutes(duracion),
            peticion.CupoMaximo,
            JsonSerializer.Serialize(ids),
            Normalizar(peticion.Observaciones),
            cancelacion);
    }

    public Task<int> AgregarEstudianteGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, AgregarEstudianteGrupoRequest peticion, CancellationToken cancelacion)
        => _repositorio.AgregarEstudianteGrupoAsync(
            idUsuarioAccion, idGrupoCita, peticion.IdEstudiante, _reloj.Ahora, cancelacion);

    public async Task ReprogramarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, ReprogramarCitaRequest peticion, CancellationToken cancelacion)
    {
        var grupo = await _repositorio.ObtenerGrupoAsync(idGrupoCita, cancelacion)
            ?? throw new ReglaNegocioException("El grupo indicado no existe.");

        var inicio = SinSegundos(peticion.FechaHoraInicio!.Value);

        ValidarQueSeaFutura(inicio, "La nueva hora tiene que ser posterior a la actual.");

        var duracion = await ObtenerDuracionAsync(grupo.IdTipoSesion, cancelacion);

        await _repositorio.ReprogramarGrupoAsync(
            idUsuarioAccion, idGrupoCita, inicio, inicio.AddMinutes(duracion), _reloj.Ahora, cancelacion);
    }

    public Task CancelarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, CancelarCitaRequest peticion, CancellationToken cancelacion)
        => _repositorio.CancelarGrupoAsync(
            idUsuarioAccion, idGrupoCita, peticion.Motivo.Trim(), _reloj.Ahora, cancelacion);

    public Task EditarGrupoAsync(
        int idUsuarioAccion, int idGrupoCita, EditarGrupoRequest peticion, CancellationToken cancelacion)
        => _repositorio.EditarGrupoAsync(
            idUsuarioAccion, idGrupoCita, Normalizar(peticion.Nombre), peticion.CupoMaximo, cancelacion);

    // =====================================================
    // REGLAS
    // =====================================================

    // Que acciones ofrece la pantalla para cada cita
    private static CitaAgendaResponse Describir(CitaAgendaFila fila, DateTime ahora)
    {
        var pendiente = fila.IdEstadoCita is EstadoProgramada or EstadoConfirmada;
        var yaEmpezo = fila.FechaHoraInicio <= ahora;

        return new CitaAgendaResponse(fila)
        {
            // Una cita de grupo no se mueve sola: se reprograma el grupo
            PuedeReprogramar = pendiente && !yaEmpezo && fila.IdGrupoCita is null,
            PuedeCancelar = pendiente && !yaEmpezo,
            PuedeMarcarNoAsistio = yaEmpezo
                && !fila.TieneSesion
                && (fila.IdEstadoCita is EstadoProgramada or EstadoConfirmada or EstadoCompletada)
        };
    }

    // Un grupo se gestiona (reprogramar, cancelar todo, agregar) si tiene al menos
    // un estudiante pendiente y todavia no empieza
    private static IReadOnlyList<CitaAgendaResponse> MarcarGruposGestionables(List<CitaAgendaResponse> citas)
    {
        var gestionables = citas
            .Where(c => c.IdGrupoCita is not null && c.PuedeCancelar)
            .Select(c => c.IdGrupoCita!.Value)
            .ToHashSet();

        return citas
            .Select(c => c.IdGrupoCita is int idGrupo && gestionables.Contains(idGrupo)
                ? c with { PuedeGestionarGrupo = true }
                : c)
            .ToList();
    }

    private void ValidarQueSeaFutura(DateTime fechaHora, string mensaje)
    {
        if (fechaHora <= _reloj.Ahora)
        {
            throw new ReglaNegocioException(mensaje);
        }
    }

    private async Task<CitaResumenFila> ObtenerCitaAsync(int idCita, CancellationToken cancelacion)
        => await _repositorio.ObtenerAsync(idCita, cancelacion)
           ?? throw new ReglaNegocioException("La cita indicada no existe.");

    private async Task<int> ObtenerDuracionAsync(int idTipoSesion, CancellationToken cancelacion)
    {
        var tipos = await _repositorio.ListarTiposSesionAsync(cancelacion);

        var tipo = tipos.FirstOrDefault(t => t.IdTipoSesion == idTipoSesion)
            ?? throw new ReglaNegocioException("El tipo de sesión indicado no existe.");

        return tipo.DuracionMinutos;
    }

    private static DateTime SinSegundos(DateTime valor)
        => new(valor.Year, valor.Month, valor.Day, valor.Hour, valor.Minute, 0, DateTimeKind.Unspecified);

    private static string? Normalizar(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}