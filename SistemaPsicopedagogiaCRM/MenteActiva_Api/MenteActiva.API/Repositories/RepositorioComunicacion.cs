using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Lo unico que habla con la base. Las reglas de datos estan en los
// procedimientos; la sustitucion de marcadores y el envio, en el servicio.
public sealed class RepositorioComunicacion : RepositorioBase, IRepositorioComunicacion
{
    public RepositorioComunicacion(IConfiguration config)
        : base(config)
    {
    }

    // ---------- consultas ----------

    // SP_Correo_ListarDestinatarios devuelve dos resultados: encargados y
    // estudiantes. El cruce se hace aqui para que el servicio y la pantalla
    // reciban un solo arbol.
    public Task<IReadOnlyList<DestinatarioCorreoResponse>> ListarDestinatariosAsync(
        CancellationToken cancelacion)
        => ConsultarVariosAsync<IReadOnlyList<DestinatarioCorreoResponse>>(
            "SP_Correo_ListarDestinatarios",
            new DynamicParameters(),
            async lector =>
            {
                var encargados = (await lector.ReadAsync<DestinatarioCorreoResponse>()).AsList();
                var estudiantes = (await lector.ReadAsync<EstudianteDestinatarioResponse>()).AsList();

                var porEncargado = estudiantes
                    .GroupBy(e => e.IdEncargado)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var encargado in encargados)
                {
                    if (porEncargado.TryGetValue(encargado.IdEncargado, out var suyos))
                    {
                        encargado.Estudiantes = suyos;
                    }
                }

                return encargados;
            },
            cancelacion);

    public Task<IReadOnlyList<CitaCorreoResponse>> ListarCitasEstudianteAsync(
        int idEstudiante, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdEstudiante", idEstudiante);

        return ConsultarAsync<CitaCorreoResponse>(
            "SP_Correo_ListarCitasEstudiante", parametros, cancelacion);
    }

    public Task<IReadOnlyList<NotificacionResponse>> ListarNotificacionesAsync(
        bool soloManuales, int? limite, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_SoloManuales", soloManuales);
        parametros.Add("p_Limite", limite);

        return ConsultarAsync<NotificacionResponse>("SP_Notificacion_Listar", parametros, cancelacion);
    }

    // ---------- escrituras ----------

    public Task<int> RegistrarCorreoAsync(
        int idUsuarioRegistro, int idEncargado, int? idCita, string correoDestino,
        string asunto, string cuerpo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioRegistro", idUsuarioRegistro);
        parametros.Add("p_IdEncargado", idEncargado);
        parametros.Add("p_IdCita", idCita);
        parametros.Add("p_CorreoDestino", correoDestino.Trim());
        parametros.Add("p_Asunto", asunto.Trim());
        parametros.Add("p_Cuerpo", cuerpo.Trim());

        return EscalarAsync("SP_Correo_Registrar", parametros, cancelacion);
    }

    public Task MarcarEnviadaAsync(int idNotificacion, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdNotificacion", idNotificacion);

        return EjecutarAsync("SP_Notificacion_MarcarEnviada", parametros, cancelacion);
    }

    public Task MarcarFallidaAsync(
        int idNotificacion, string mensajeError, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdNotificacion", idNotificacion);
        parametros.Add("p_MensajeError", mensajeError);

        return EjecutarAsync("SP_Notificacion_MarcarFallida", parametros, cancelacion);
    }

    // ---------- avisos automaticos ----------

    public Task<int> ProgramarRecordatoriosAsync(CancellationToken cancelacion)
        => EscalarAsync("SP_Notificacion_ProgramarRecordatorios", new DynamicParameters(), cancelacion);

    public Task<IReadOnlyList<NotificacionPendienteResponse>> ObtenerPendientesAsync(
        int maxIntentos, int limite, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_MaxIntentos", maxIntentos);
        parametros.Add("p_Limite", limite);

        return ConsultarAsync<NotificacionPendienteResponse>(
            "SP_Notificacion_ObtenerPendientes", parametros, cancelacion);
    }
}
