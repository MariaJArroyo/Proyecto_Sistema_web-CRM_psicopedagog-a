using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Lo unico que habla con la base. Antes esto vivia dentro de
// SolicitudesController, que abria su propia conexion y armaba los parametros
// ahi mismo (RG-92 y RG-117).
//
// Los procedimientos son los mismos de siempre y no se tocaron: ya escriben la
// bitacora dentro de su transaccion y ya lanzan SIGNAL 45000, que RepositorioBase
// convierte en ReglaNegocioException.
public sealed class RepositorioSolicitud : RepositorioBase, IRepositorioSolicitud
{
    public RepositorioSolicitud(IConfiguration config)
        : base(config)
    {
    }

    // ---------- consultas ----------

    public Task<IReadOnlyList<SolicitudResponse>> ListarAsync(CancellationToken cancelacion)
        => ConsultarAsync<SolicitudResponse>(
            "SP_ConsultarSolicitudes_CRM", new DynamicParameters(), cancelacion);

    // SP_ObtenerSolicitud_CRM devuelve dos resultados: la solicitud y los
    // clientes con el mismo telefono o correo. El cruce se hace aqui para que el
    // servicio y la pantalla reciban un solo arbol, igual que en
    // RepositorioComunicacion con los destinatarios.
    public Task<SolicitudDetalleResponse?> ObtenerAsync(
        int idSolicitud, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdSolicitud", idSolicitud);

        return ConsultarVariosAsync<SolicitudDetalleResponse?>(
            "SP_ObtenerSolicitud_CRM",
            parametros,
            async lector =>
            {
                var solicitud = await lector.ReadFirstOrDefaultAsync<SolicitudDetalleResponse>();

                if (solicitud is null)
                {
                    return null;
                }

                solicitud.Coincidencias =
                    (await lector.ReadAsync<ClienteCoincidenteResponse>()).AsList();

                return solicitud;
            },
            cancelacion);
    }

    public Task<int> ContarPendientesAsync(CancellationToken cancelacion)
        => EscalarAsync("SP_ContarSolicitudesPendientes_CRM", new DynamicParameters(), cancelacion);

    public Task<IReadOnlyList<EstadoSolicitudResponse>> ListarEstadosAsync(
        CancellationToken cancelacion)
        => ConsultarAsync<EstadoSolicitudResponse>(
            "SP_ListarEstadosSolicitud_CRM", new DynamicParameters(), cancelacion);

    // ---------- escrituras ----------

    public Task CambiarEstadoAsync(
        int idUsuarioAccion, int idSolicitud, CambiarEstadoSolicitudRequest peticion,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdSolicitud", idSolicitud);
        parametros.Add("p_IdEstadoSolicitud", peticion.IdEstadoSolicitud);
        parametros.Add("p_NotaInterna", peticion.NotaInterna?.Trim());

        return EjecutarAsync("SP_CambiarEstadoSolicitud_CRM", parametros, cancelacion);
    }

    public Task VincularClienteAsync(
        int idUsuarioAccion, int idSolicitud, int idEncargado, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdSolicitud", idSolicitud);
        parametros.Add("p_IdEncargado", idEncargado);

        return EjecutarAsync("SP_VincularSolicitudCliente_CRM", parametros, cancelacion);
    }
}
