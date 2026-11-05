using System.Globalization;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Orquesta el correo manual. El orden importa y es a proposito:
//
//   1. se valida que el destinatario exista, este activo y tenga correo, y que
//      el estudiante y la cita que vengan sean suyos
//   2. se sustituyen los marcadores
//   3. si quedo alguno sin resolver, se para aqui y no se registra nada
//   4. se registra la notificacion como pendiente
//   5. se intenta mandar
//   6. se marca enviada o fallida
//
// El paso 3 va antes del 4 para que un texto que no se puede armar no deje
// basura en la bitacora. Y el 4 va antes del 5 para que un correo que si salio
// nunca quede sin rastro: si el proceso se cae entre los dos, queda una fila
// pendiente, que es el error que se puede ver y arreglar.
public sealed class ServicioComunicacion : IServicioComunicacion
{
    private const int LimitePorDefecto = 50;

    private readonly IRepositorioComunicacion _repositorio;
    private readonly IRepositorioPlantilla _plantillas;
    private readonly IServicioConfiguracion _configuracion;
    private readonly IServicioCorreo _correo;
    private readonly ILogger<ServicioComunicacion> _registro;

    public ServicioComunicacion(
        IRepositorioComunicacion repositorio,
        IRepositorioPlantilla plantillas,
        IServicioConfiguracion configuracion,
        IServicioCorreo correo,
        ILogger<ServicioComunicacion> registro)
    {
        _repositorio = repositorio;
        _plantillas = plantillas;
        _configuracion = configuracion;
        _correo = correo;
        _registro = registro;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    public async Task<CatalogosCorreoResponse> ObtenerCatalogosAsync(CancellationToken cancelacion)
    {
        var destinatarios = await _repositorio.ListarDestinatariosAsync(cancelacion);

        // Solo las activas: al redactar no se ofrece una plantilla apagada
        var plantillas = await _plantillas.ListarAsync(true, cancelacion);

        return new CatalogosCorreoResponse
        {
            Destinatarios = destinatarios,
            Plantillas = plantillas,
            Marcadores = MarcadoresPlantilla.ParaMostrar()
        };
    }

    public Task<IReadOnlyList<NotificacionResponse>> ListarNotificacionesAsync(
        bool soloManuales, int? limite, CancellationToken cancelacion)
        => _repositorio.ListarNotificacionesAsync(
            soloManuales, limite is > 0 ? limite : LimitePorDefecto, cancelacion);

    public Task<IReadOnlyList<CitaCorreoResponse>> ListarCitasEstudianteAsync(
        int idEstudiante, CancellationToken cancelacion)
        => _repositorio.ListarCitasEstudianteAsync(idEstudiante, cancelacion);

    // =====================================================
    // VISTA PREVIA
    // =====================================================

    public async Task<VistaPreviaCorreoResponse> VistaPreviaAsync(
        EnviarCorreoRequest peticion, CancellationToken cancelacion)
    {
        var (destinatario, estudiante, cita) = await ResolverAsync(peticion, cancelacion);
        var valores = await ValoresAsync(destinatario, estudiante, cita, cancelacion);

        return Armar(peticion, valores, cita is not null);
    }

    // =====================================================
    // ENVIO
    // =====================================================

    public async Task<CorreoEnviadoResponse> EnviarCorreoAsync(
        int idUsuarioAccion, EnviarCorreoRequest peticion, CancellationToken cancelacion)
    {
        var (destinatario, estudiante, cita) = await ResolverAsync(peticion, cancelacion);
        var valores = await ValoresAsync(destinatario, estudiante, cita, cancelacion);

        var vista = Armar(peticion, valores, cita is not null);

        if (!vista.SePuedeEnviar)
        {
            throw new ReglaNegocioException(vista.Advertencia!);
        }

        var idNotificacion = await _repositorio.RegistrarCorreoAsync(
            idUsuarioAccion, destinatario.IdEncargado, cita?.IdCita, destinatario.Correo,
            vista.Asunto, vista.Cuerpo, cancelacion);

        try
        {
            var datos = await _configuracion.ObtenerDatosConsultorioAsync(cancelacion);

            var html = MensajeComunicacion.Armar(vista.Asunto, vista.Cuerpo, datos.NombreConsultorio);

            await _correo.EnviarAsync(destinatario.Correo, vista.Asunto, html);

            await _repositorio.MarcarEnviadaAsync(idNotificacion, cancelacion);

            return new CorreoEnviadoResponse
            {
                IdNotificacion = idNotificacion,
                CorreoDestino = destinatario.Correo,
                Mensaje = $"Correo enviado a {destinatario.Correo}."
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // El motivo tecnico va al registro y a la fila; a la persona se le
            // dice en una frase que no salio y donde verlo. Sale como 400 y no
            // como 500 a proposito: el 500 no lleva mensaje y la pantalla
            // quedaria diciendo "error" sin explicar que el correo no se mando
            // pero si quedo anotado.
            _registro.LogError(ex, "No se pudo enviar el correo {IdNotificacion}.", idNotificacion);

            await _repositorio.MarcarFallidaAsync(idNotificacion, ex.Message, cancelacion);

            throw new ReglaNegocioException(
                "El correo quedó registrado pero no se pudo enviar. "
                + "Aparece como fallido en el historial y se puede volver a intentar.", ex);
        }
    }

    // =====================================================
    // ARMADO
    // =====================================================

    private static VistaPreviaCorreoResponse Armar(
        EnviarCorreoRequest peticion, IReadOnlyDictionary<string, string?> valores, bool hayCita)
    {
        var asunto = MarcadoresPlantilla.Sustituir(peticion.Asunto, valores);
        var cuerpo = MarcadoresPlantilla.Sustituir(peticion.Cuerpo, valores);

        var sinResolver = MarcadoresPlantilla.SinResolver(asunto)
            .Concat(MarcadoresPlantilla.SinResolver(cuerpo))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new VistaPreviaCorreoResponse
        {
            Asunto = asunto,
            Cuerpo = cuerpo,
            SinResolver = sinResolver,
            Advertencia = sinResolver.Count == 0 ? null : Explicar(sinResolver, hayCita)
        };
    }

    // Se explica por que no se pudo resolver cada uno, porque el motivo cambia:
    // [Estudiante] se arregla eligiendo un estudiante y [Fecha] eligiendo una
    // cita, pero [Motivo] no lo arregla ninguna cita que no este cancelada.
    private static string Explicar(IReadOnlyList<string> sinResolver, bool hayCita)
    {
        var lista = MarcadoresPlantilla.Enumerar(sinResolver);

        var faltaEstudiante = sinResolver.Any(
            m => string.Equals(m, MarcadoresPlantilla.Estudiante, StringComparison.OrdinalIgnoreCase));

        var deCita = sinResolver.Where(m =>
                string.Equals(m, MarcadoresPlantilla.Fecha, StringComparison.OrdinalIgnoreCase)
                || string.Equals(m, MarcadoresPlantilla.Hora, StringComparison.OrdinalIgnoreCase)
                || string.Equals(m, MarcadoresPlantilla.Motivo, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var texto = $"El correo no se envió porque quedaron marcadores sin resolver: {lista}.";

        if (faltaEstudiante)
        {
            texto += " Para usar [Estudiante] hay que elegir un estudiante del destinatario.";
        }

        if (deCita.Count > 0)
        {
            // Con una cita elegida, [Fecha] y [Hora] siempre salen, asi que lo
            // unico que puede quedar colgando ahi es [Motivo]
            texto += hayCita
                ? $" {MarcadoresPlantilla.Enumerar(deCita)} no sale de la cita "
                    + "elegida: solo una cita cancelada tiene motivo."
                : $" Para usar {MarcadoresPlantilla.Enumerar(deCita)} hay que "
                    + "elegir la cita de la que habla el correo.";
        }

        return texto;
    }

    private async Task<IReadOnlyDictionary<string, string?>> ValoresAsync(
        DestinatarioCorreoResponse destinatario,
        EstudianteDestinatarioResponse? estudiante,
        CitaCorreoResponse? cita,
        CancellationToken cancelacion)
    {
        var datos = await _configuracion.ObtenerDatosConsultorioAsync(cancelacion);

        // El mismo formato que arma FN_Plantilla_Sustituir en la base, para que un
        // recordatorio automatico y un correo escrito a mano sobre la misma cita
        // digan la fecha igual. Invariante a proposito: con la cultura del
        // servidor, dd/MM se podria dar vuelta
        var fecha = cita?.FechaHoraInicio.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var hora = cita?.FechaHoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture);

        // Lo que no se puede resolver NO se mete con cadena vacia: se deja fuera
        // para que Sustituir lo conserve y SinResolver lo detecte
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [MarcadoresPlantilla.Encargado] = destinatario.Encargado,
            [MarcadoresPlantilla.Estudiante] = estudiante?.Estudiante,
            [MarcadoresPlantilla.Consultorio] = datos.NombreConsultorio,
            [MarcadoresPlantilla.Fecha] = fecha,
            [MarcadoresPlantilla.Hora] = hora,
            [MarcadoresPlantilla.Motivo] = cita?.MotivoCancelacion
        };
    }

    // El destinatario se busca en la misma lista que ve la pantalla, que ya
    // excluye al inactivo y al que no tiene correo. Asi la validacion del
    // escenario 2 de la historia no depende de que la pantalla haya mandado
    // bien el id: si no esta en la lista, no se le escribe.
    private async Task<(DestinatarioCorreoResponse, EstudianteDestinatarioResponse?, CitaCorreoResponse?)>
        ResolverAsync(EnviarCorreoRequest peticion, CancellationToken cancelacion)
    {
        var destinatarios = await _repositorio.ListarDestinatariosAsync(cancelacion);

        var destinatario = destinatarios.FirstOrDefault(d => d.IdEncargado == peticion.IdEncargado)
            ?? throw new ReglaNegocioException(
                "El destinatario indicado no existe, está inactivo o no tiene correo registrado.");

        if (peticion.IdEstudiante is null)
        {
            // Sin estudiante no hay de donde colgar una cita: la cita es de el
            if (peticion.IdCita is not null)
            {
                throw new ReglaNegocioException(
                    "Para elegir una cita hay que elegir primero el estudiante.");
            }

            return (destinatario, null, null);
        }

        var estudiante = destinatario.Estudiantes
            .FirstOrDefault(e => e.IdEstudiante == peticion.IdEstudiante)
            ?? throw new ReglaNegocioException(
                "El estudiante indicado no está a cargo de ese destinatario.");

        if (peticion.IdCita is null)
        {
            return (destinatario, estudiante, null);
        }

        // Igual que con el destinatario: la cita se busca en la misma lista que ve
        // la pantalla. Asi una cita de otro estudiante no entra ni mandando el id
        // a mano
        var citas = await _repositorio.ListarCitasEstudianteAsync(
            estudiante.IdEstudiante, cancelacion);

        var cita = citas.FirstOrDefault(c => c.IdCita == peticion.IdCita)
            ?? throw new ReglaNegocioException(
                "La cita indicada no es de ese estudiante o quedó fuera del período que se ofrece.");

        return (destinatario, estudiante, cita);
    }
}
