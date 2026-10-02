using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Reglas de formato de la configuracion. Las reglas que dependen de los datos
// (citas en un dia, nombres repetidos) las validan las SPs.
public sealed class ServicioConfiguracion : IServicioConfiguracion
{
    private const string FormatoHora = "HH:mm";

    // Los espacios de la agenda avanzan cada 30 minutos: las franjas deben calzar con eso
    private const int PasoAgendaMinutos = 30;
    private const int MaximoAniosDiaNoLaboral = 2;
    private const int DigitosTelefono = 8;
    private const int HorasRecordatorioPorDefecto = 24;

    private const string ClaveNombre = "NombreConsultorio";
    private const string ClaveCorreo = "CorreoContacto";
    private const string ClaveTelefono = "TelefonoContacto";
    private const string ClaveDireccion = "DireccionConsultorio";
    private const string ClaveMoneda = "Moneda";
    private const string ClaveHorasRecordatorio = "HorasRecordatorioCita";

    private static readonly string[] NombresDias =
        ["", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];

    private readonly IRepositorioConfiguracion _repositorio;
    private readonly IRelojNegocio _reloj;

    public ServicioConfiguracion(IRepositorioConfiguracion repositorio, IRelojNegocio reloj)
    {
        _repositorio = repositorio;
        _reloj = reloj;
    }

    // =====================================================
    // HORARIO DE ATENCION
    // =====================================================

    public async Task<IReadOnlyList<FranjaHorarioResponse>> ListarHorarioAsync(CancellationToken cancelacion)
    {
        var filas = await _repositorio.ListarHorarioAsync(cancelacion);

        return filas
            .Select(f => new FranjaHorarioResponse
            {
                DiaSemana = f.DiaSemana,
                HoraInicio = TimeOnly.FromTimeSpan(f.HoraInicio).ToString(FormatoHora, CultureInfo.InvariantCulture),
                HoraFin = TimeOnly.FromTimeSpan(f.HoraFin).ToString(FormatoHora, CultureInfo.InvariantCulture)
            })
            .ToList();
    }

    public async Task<GuardarHorarioResponse> GuardarHorarioAsync(
        int idUsuarioAccion, GuardarHorarioRequest peticion, CancellationToken cancelacion)
    {
        var franjas = peticion.Franjas
            .Select(LeerFranja)
            .OrderBy(f => f.DiaSemana)
            .ThenBy(f => f.Inicio)
            .ToList();

        ValidarTraslapes(franjas);

        var json = JsonSerializer.Serialize(franjas.Select(f => new
        {
            f.DiaSemana,
            HoraInicio = f.Inicio.ToString(FormatoHora, CultureInfo.InvariantCulture),
            HoraFin = f.Fin.ToString(FormatoHora, CultureInfo.InvariantCulture)
        }));

        var citasFuera = await _repositorio.GuardarHorarioAsync(idUsuarioAccion, json, _reloj.Ahora, cancelacion);

        var mensaje = citasFuera.Count switch
        {
            0 => "Horario guardado correctamente.",
            1 => "Horario guardado. Hay 1 cita futura fuera del nuevo horario: conviene reprogramarla.",
            _ => $"Horario guardado. Hay {citasFuera.Count} citas futuras fuera del nuevo horario: conviene reprogramarlas."
        };

        return new GuardarHorarioResponse(mensaje, citasFuera);
    }

    private static FranjaValidada LeerFranja(FranjaHorarioRequest franja)
    {
        var dia = NombresDias[franja.DiaSemana];

        if (!TryLeerHora(franja.HoraInicio, out var inicio) || !TryLeerHora(franja.HoraFin, out var fin))
        {
            throw new ReglaNegocioException($"Las horas del {dia} deben tener el formato HH:mm.");
        }

        if (fin <= inicio)
        {
            throw new ReglaNegocioException(
                $"En el {dia}, la hora de cierre debe ser posterior a la de apertura.");
        }

        if (inicio.Minute % PasoAgendaMinutos != 0 || fin.Minute % PasoAgendaMinutos != 0)
        {
            throw new ReglaNegocioException(
                $"En el {dia}, use horas en punto o y media (por ejemplo 08:00 o 08:30).");
        }

        return new FranjaValidada(franja.DiaSemana, inicio, fin);
    }

    // La SP tambien lo valida; aqui se hace antes para dar un mensaje con el dia
    private static void ValidarTraslapes(IReadOnlyList<FranjaValidada> franjasOrdenadas)
    {
        for (var i = 1; i < franjasOrdenadas.Count; i++)
        {
            var anterior = franjasOrdenadas[i - 1];
            var actual = franjasOrdenadas[i];

            if (actual.DiaSemana == anterior.DiaSemana && actual.Inicio < anterior.Fin)
            {
                throw new ReglaNegocioException(
                    $"El {NombresDias[actual.DiaSemana]} tiene franjas que se traslapan.");
            }
        }
    }

    private static bool TryLeerHora(string texto, out TimeOnly hora)
        => TimeOnly.TryParseExact(texto?.Trim(), FormatoHora, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out hora);

    private sealed record FranjaValidada(int DiaSemana, TimeOnly Inicio, TimeOnly Fin);

    // =====================================================
    // DIAS NO LABORALES
    // =====================================================

    public Task<IReadOnlyList<DiaNoLaboralResponse>> ListarDiasNoLaboralesAsync(CancellationToken cancelacion)
            => ListarDiasNoLaboralesAsync(_reloj.Hoy, DateOnly.MaxValue, cancelacion);

    // hasta es exclusivo, igual que en la consulta de la agenda
    public async Task<IReadOnlyList<DiaNoLaboralResponse>> ListarDiasNoLaboralesAsync(
        DateOnly desde, DateOnly hasta, CancellationToken cancelacion)
    {
        if (hasta <= desde)
        {
            throw new ReglaNegocioException("El rango de fechas no es válido.");
        }

        var filas = await _repositorio.ListarDiasNoLaboralesAsync(
            desde.ToDateTime(TimeOnly.MinValue), cancelacion);

        return filas
            .Select(f => new DiaNoLaboralResponse
            {
                IdDiaNoLaboral = f.IdDiaNoLaboral,
                Fecha = DateOnly.FromDateTime(f.Fecha),
                Motivo = f.Motivo,
                EsFeriado = f.EsFeriado
            })
            .Where(d => d.Fecha < hasta)
            .ToList();
    }

    public Task<int> AgregarDiaNoLaboralAsync(
        int idUsuarioAccion, AgregarDiaNoLaboralRequest peticion, CancellationToken cancelacion)
    {
        var fecha = peticion.Fecha!.Value;
        var hoy = _reloj.Hoy;

        if (fecha < hoy)
        {
            throw new ReglaNegocioException("Solo se pueden marcar fechas de hoy en adelante.");
        }

        if (fecha > hoy.AddYears(MaximoAniosDiaNoLaboral))
        {
            throw new ReglaNegocioException(
                $"Solo se pueden marcar fechas dentro de los próximos {MaximoAniosDiaNoLaboral} años.");
        }

        return _repositorio.AgregarDiaNoLaboralAsync(
            idUsuarioAccion,
            fecha.ToDateTime(TimeOnly.MinValue),
            peticion.Motivo.Trim(),
            peticion.EsFeriado,
            hoy.ToDateTime(TimeOnly.MinValue),
            cancelacion);
    }

    public Task EliminarDiaNoLaboralAsync(int idUsuarioAccion, int idDiaNoLaboral, CancellationToken cancelacion)
        => _repositorio.EliminarDiaNoLaboralAsync(idUsuarioAccion, idDiaNoLaboral, cancelacion);

    // =====================================================
    // TIPOS DE SESION
    // =====================================================

    public Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion)
        => _repositorio.ListarTiposSesionAsync(cancelacion);

    public Task<int> CrearTipoSesionAsync(
        int idUsuarioAccion, GuardarTipoSesionRequest peticion, CancellationToken cancelacion)
        => _repositorio.GuardarTipoSesionAsync(
            idUsuarioAccion, null, peticion.Nombre.Trim(), peticion.DuracionMinutos, cancelacion);

    public async Task EditarTipoSesionAsync(
        int idUsuarioAccion, int idTipoSesion, GuardarTipoSesionRequest peticion, CancellationToken cancelacion)
        => await _repositorio.GuardarTipoSesionAsync(
            idUsuarioAccion, idTipoSesion, peticion.Nombre.Trim(), peticion.DuracionMinutos, cancelacion);

    // =====================================================
    // DATOS DEL CONSULTORIO
    // =====================================================

    public async Task<DatosConsultorioResponse> ObtenerDatosConsultorioAsync(CancellationToken cancelacion)
    {
        var filas = await _repositorio.ListarConfiguracionAsync(cancelacion);
        var valores = filas.ToDictionary(f => f.Clave, f => f.Valor, StringComparer.OrdinalIgnoreCase);

        return new DatosConsultorioResponse
        {
            NombreConsultorio = Valor(valores, ClaveNombre),
            CorreoContacto = Valor(valores, ClaveCorreo),
            TelefonoContacto = Valor(valores, ClaveTelefono),
            DireccionConsultorio = Valor(valores, ClaveDireccion),
            Moneda = Valor(valores, ClaveMoneda),
            HorasRecordatorioCita = int.TryParse(Valor(valores, ClaveHorasRecordatorio), out var horas)
                ? horas
                : HorasRecordatorioPorDefecto
        };
    }

    public Task GuardarDatosConsultorioAsync(
        int idUsuarioAccion, GuardarDatosConsultorioRequest peticion, CancellationToken cancelacion)
    {
        var correo = peticion.CorreoContacto.Trim();

        if (!MailAddress.TryCreate(correo, out var direccion) || direccion.Address != correo)
        {
            throw new ReglaNegocioException("El correo de contacto no tiene un formato válido.");
        }

        var telefono = new string(peticion.TelefonoContacto.Where(char.IsDigit).ToArray());

        if (telefono.Length != DigitosTelefono)
        {
            throw new ReglaNegocioException($"El teléfono debe tener {DigitosTelefono} dígitos.");
        }

        var cambios = new[]
        {
            new { Clave = ClaveNombre, Valor = peticion.NombreConsultorio.Trim() },
            new { Clave = ClaveCorreo, Valor = correo },
            new { Clave = ClaveTelefono, Valor = telefono },
            new { Clave = ClaveDireccion, Valor = peticion.DireccionConsultorio.Trim() },
            new { Clave = ClaveHorasRecordatorio, Valor = peticion.HorasRecordatorioCita.ToString(CultureInfo.InvariantCulture) }
        };

        return _repositorio.GuardarConfiguracionAsync(idUsuarioAccion, JsonSerializer.Serialize(cambios), cancelacion);
    }

    private static string Valor(IReadOnlyDictionary<string, string> valores, string clave)
        => valores.TryGetValue(clave, out var valor) ? valor : string.Empty;
}