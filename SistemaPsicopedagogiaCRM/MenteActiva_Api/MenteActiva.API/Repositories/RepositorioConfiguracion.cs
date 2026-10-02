using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Solo llama SPs de configuracion. Las validaciones de formato estan en
// ServicioConfiguracion; las de datos (traslapes, citas en el dia...) en las SPs.
public sealed class RepositorioConfiguracion : RepositorioBase, IRepositorioConfiguracion
{
    public RepositorioConfiguracion(IConfiguration config)
        : base(config)
    {
    }

    // ---------- Horario ----------

    public Task<IReadOnlyList<FranjaHorarioFila>> ListarHorarioAsync(CancellationToken cancelacion)
        => ConsultarAsync<FranjaHorarioFila>("SP_Horario_Listar_CRM", null, cancelacion);

    public Task<IReadOnlyList<CitaFueraDeHorarioResponse>> GuardarHorarioAsync(
        int idUsuarioAccion, string franjasJson, DateTime ahora, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_FranjasJson", franjasJson);
        parametros.Add("p_Ahora", ahora);

        // La SP guarda y al final devuelve las citas que quedaron fuera del horario
        return ConsultarAsync<CitaFueraDeHorarioResponse>("SP_Horario_Guardar_CRM", parametros, cancelacion);
    }

    // ---------- Dias no laborales ----------

    public Task<IReadOnlyList<DiaNoLaboralFila>> ListarDiasNoLaboralesAsync(
        DateTime desde, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_Desde", desde.Date);

        return ConsultarAsync<DiaNoLaboralFila>("SP_DiaNoLaboral_Listar_CRM", parametros, cancelacion);
    }

    public Task<int> AgregarDiaNoLaboralAsync(
        int idUsuarioAccion, DateTime fecha, string motivo, bool esFeriado, DateTime hoy,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_Fecha", fecha.Date);
        parametros.Add("p_Motivo", motivo);
        parametros.Add("p_EsFeriado", esFeriado);
        parametros.Add("p_Hoy", hoy.Date);

        return EscalarAsync("SP_DiaNoLaboral_Agregar_CRM", parametros, cancelacion);
    }

    public Task EliminarDiaNoLaboralAsync(int idUsuarioAccion, int idDiaNoLaboral, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdDiaNoLaboral", idDiaNoLaboral);

        return EjecutarAsync("SP_DiaNoLaboral_Eliminar_CRM", parametros, cancelacion);
    }

    // ---------- Tipos de sesion ----------

    public Task<IReadOnlyList<TipoSesionResponse>> ListarTiposSesionAsync(CancellationToken cancelacion)
        => ConsultarAsync<TipoSesionResponse>("SP_ListarTiposSesion_CRM", null, cancelacion);

    public Task<int> GuardarTipoSesionAsync(
        int idUsuarioAccion, int? idTipoSesion, string nombre, int duracionMinutos,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdTipoSesion", idTipoSesion);
        parametros.Add("p_Nombre", nombre);
        parametros.Add("p_DuracionMinutos", duracionMinutos);

        return EscalarAsync("SP_TipoSesion_Guardar_CRM", parametros, cancelacion);
    }

    // ---------- Datos del consultorio ----------

    public Task<IReadOnlyList<ConfiguracionFila>> ListarConfiguracionAsync(CancellationToken cancelacion)
        => ConsultarAsync<ConfiguracionFila>("SP_Configuracion_Listar_CRM", null, cancelacion);

    public Task GuardarConfiguracionAsync(int idUsuarioAccion, string valoresJson, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_ValoresJson", valoresJson);

        return EjecutarAsync("SP_Configuracion_Guardar_CRM", parametros, cancelacion);
    }
}