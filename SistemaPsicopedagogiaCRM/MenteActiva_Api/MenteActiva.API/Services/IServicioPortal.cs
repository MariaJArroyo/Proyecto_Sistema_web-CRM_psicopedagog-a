using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioPortal
{
    Task<PortalResponse> ObtenerInicioAsync(int idUsuario,int? idEstudiante, CancellationToken cancelacion);

    Task<IReadOnlyList<PortalCitaResponse>> ListarCitasAsync(int idUsuario, int idEstudiante, string? filtro, CancellationToken cancelacion);
    Task<PortalProgresoResponse> ListarProgresoAsync(int idUsuario, int idEstudiante,string? filtroReportes,CancellationToken cancelacion);
    Task<IReadOnlyList<PortalTareaResponse>> ListarTareasAsync(int idUsuario,int idEstudiante,string? filtro,CancellationToken cancelacion);
    Task<IReadOnlyList<PortalPagoResponse>> ListarPagosAsync(int idUsuario,int idEstudiante,string? filtro, CancellationToken cancelacion);
    Task<PortalPagoDetalleResponse> ObtenerPagoAsync(int idUsuario,int idEstudiante,int idPago, CancellationToken cancelacion);
    Task<PortalArchivoTareaResponse> ObtenerArchivoTareaAsync(int idUsuario,int idEstudiante,int idTareaAsignacion,CancellationToken cancelacion);
    Task<PortalReporteDetalleResponse> ObtenerReporteAsync(int idUsuario, int idEstudiante, int idReporte, CancellationToken cancelacion);


    Task<PortalPerfilResponse> ObtenerPerfilAsync(int idUsuario, CancellationToken cancelacion);
    Task ActualizarPerfilAsync(int idUsuario, EditarMiPerfilRequest peticion, CancellationToken cancelacion);

    // Lo publicado y activo.
    Task<IReadOnlyList<PortalRecursoResponse>> ListarRecursosAsync(int idUsuario, CancellationToken cancelacion);

    Task<ArchivoDescarga?> AbrirArchivoTareaAsync(int idUsuario, int idEstudiante, int idTareaAsignacion, CancellationToken cancelacion);

    Task<ArchivoDescarga?> ObtenerArchivoRecursoAsync(int idUsuario, int idMaterial, CancellationToken cancelacion);

    Task<VerificacionEnlaceResponse> VerificarEnlaceRecursoAsync(int idUsuario, int idMaterial, CancellationToken cancelacion);


}