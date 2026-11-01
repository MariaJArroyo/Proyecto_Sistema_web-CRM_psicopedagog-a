using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public class RepositorioPortal : RepositorioBase, IRepositorioPortal
{
    public RepositorioPortal(IConfiguration config)
        : base(config)
    {
    }

    public Task<PortalResponse> ObtenerInicioAsync(
        int idUsuario,int? idEstudiante,CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);

        return ConsultarVariosAsync<PortalResponse>(
            "SP_Portal_ObtenerInicio",parametros,
            async lector =>
            {
                // Primer resultado: estudiantes del encargado.
                var estudiantes = (await lector.ReadAsync<PortalEstudianteResponse>()).AsList();

                // Segundo resultado: resumen del estudiante seleccionado.
                var resumen = await lector.ReadSingleAsync<PortalResumenResponse>();

                return new PortalResponse
                {
                    Estudiantes = estudiantes,
                    Resumen = resumen
                };
            },
            cancelacion);
    }


    public Task<IReadOnlyList<PortalCitaResponse>> ListarCitasAsync(int idUsuario, int idEstudiante, string filtro, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Filtro", filtro);

        return ConsultarAsync<PortalCitaResponse>("SP_Portal_ListarCitas", parametros, cancelacion);
    }

    public Task<PortalProgresoResponse> ListarProgresoAsync(int idUsuario,int idEstudiante,string filtroReportes,CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_FiltroReportes", filtroReportes);

        return ConsultarVariosAsync<PortalProgresoResponse>("SP_Portal_ListarProgreso",parametros,async lector =>
            {
                // Primer resultado: sesiones, avances y recomendaciones.
                var sesiones = (await lector.ReadAsync<PortalProgresoSesionResponse>()).AsList();

                // Segundo resultado: reportes según el filtro.
                var reportes = (await lector.ReadAsync<PortalReporteListaResponse>()).AsList();

                return new PortalProgresoResponse
                {
                    Sesiones = sesiones,
                    Reportes = reportes
                };
            },
            cancelacion);
    }


    public Task<IReadOnlyList<PortalTareaResponse>> ListarTareasAsync(int idUsuario,int idEstudiante,string filtro,CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Filtro", filtro);

        return ConsultarAsync<PortalTareaResponse>( "SP_Portal_ListarTareas", parametros,cancelacion);
    }

    public Task<IReadOnlyList<PortalPagoResponse>> ListarPagosAsync(int idUsuario,int idEstudiante, string filtro,CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Filtro", filtro);

        return ConsultarAsync<PortalPagoResponse>("SP_Portal_ListarPagos", parametros, cancelacion);
    }

    public Task<PortalPagoDetalleResponse> ObtenerPagoAsync(int idUsuario,int idEstudiante,int idPago, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdPago", idPago);

        return ConsultarVariosAsync<PortalPagoDetalleResponse>("SP_Portal_ObtenerPago",parametros,async lector =>
            {
                // Primer resultado: encabezado del cargo.
                var pago = await lector
                    .ReadSingleAsync<PortalPagoResponse>();

                // Segundo resultado: abonos activos.
                var abonos = (await lector.ReadAsync<PortalAbonoResponse>()).AsList();

                return new PortalPagoDetalleResponse
                {
                    Pago = pago,
                    Abonos = abonos
                };
            },
            cancelacion);
    }
    public async Task<PortalArchivoTareaResponse?> ObtenerArchivoTareaAsync(int idUsuario,int idEstudiante,int idTareaAsignacion,CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdTareaAsignacion", idTareaAsignacion);

        var resultados = await ConsultarAsync<PortalArchivoTareaResponse>("SP_Portal_ObtenerArchivoTarea",parametros,cancelacion);

        return resultados.SingleOrDefault();
    }
    public async Task<PortalReporteDetalleResponse?> ObtenerReporteAsync(int idUsuario, int idEstudiante, int idReporte, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdReporte", idReporte);

        var resultados = await ConsultarAsync<PortalReporteDetalleResponse>("SP_Portal_ObtenerReporte", parametros, cancelacion);

        return resultados.SingleOrDefault();
    }

}