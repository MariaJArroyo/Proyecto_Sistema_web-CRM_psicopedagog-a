using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

public class ServicioPortal : IServicioPortal
{
    // Ids fijos de TB_TIPO_MATERIAL
    private const int TipoPdf = 2;
    private const int TipoEnlace = 3;

    private readonly IRepositorioPortal _repositorio;
    private readonly IAlmacenMateriales _almacen;
    private readonly IVerificadorEnlaces _verificador;

    public ServicioPortal(
        IRepositorioPortal repositorio,
        IAlmacenMateriales almacen,
        IVerificadorEnlaces verificador)
    {
        _repositorio = repositorio;
        _almacen = almacen;
        _verificador = verificador;
    }

    public Task<PortalResponse> ObtenerInicioAsync(int idUsuario,int? idEstudiante,CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException("El usuario indicado no es válido.");
        }

        if (idEstudiante.HasValue && idEstudiante.Value <= 0)
        {
            throw new ReglaNegocioException("El estudiante indicado no es válido.");
        }

        return _repositorio.ObtenerInicioAsync(idUsuario, idEstudiante,cancelacion);
    }

    public Task<IReadOnlyList<PortalCitaResponse>> ListarCitasAsync(int idUsuario, int idEstudiante, string? filtro, CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException("El usuario indicado no es válido.");
        }

        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException("Seleccione un estudiante válido.");
        }

        var filtroNormalizado = string.IsNullOrWhiteSpace(filtro)
            ? "todas"
            : filtro.Trim().ToLowerInvariant();

        var filtrosPermitidos = new[]
        {
            "futuras",
            "pasadas",
            "canceladas",
            "completadas",
            "todas"
        };

        if (!filtrosPermitidos.Contains(filtroNormalizado))
        {
            throw new ReglaNegocioException("El filtro de citas no es válido.");
        }

        return _repositorio.ListarCitasAsync(idUsuario, idEstudiante, filtroNormalizado, cancelacion);
    }

    public Task<PortalProgresoResponse> ListarProgresoAsync(int idUsuario,int idEstudiante,string? filtroReportes,CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException("El usuario indicado no es válido.");
        }

        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException( "Seleccione un estudiante válido.");
        }

        var filtroNormalizado = string.IsNullOrWhiteSpace(filtroReportes)
            ? "todos"
            : filtroReportes.Trim().ToLowerInvariant();

        var filtrosPermitidos = new[]
        {
            "nuevos",
            "viejos",
            "todos"
        };

        if (!filtrosPermitidos.Contains(filtroNormalizado))
        {
            throw new ReglaNegocioException("El filtro de reportes no es válido.");
        }

        return _repositorio.ListarProgresoAsync(idUsuario,idEstudiante,filtroNormalizado,cancelacion);
    }

    public Task<IReadOnlyList<PortalTareaResponse>> ListarTareasAsync(int idUsuario, int idEstudiante,string? filtro,CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException( "El usuario indicado no es válido.");
        }

        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException("Seleccione un estudiante válido.");
        }

        var filtroNormalizado = string.IsNullOrWhiteSpace(filtro)
            ? "todas"
            : filtro.Trim().ToLowerInvariant();

        var filtrosPermitidos = new[]
        {
            "pendientes",
            "asignadas",
            "entregadas",
            "todas"
        };

        if (!filtrosPermitidos.Contains(filtroNormalizado))
        {
            throw new ReglaNegocioException("El filtro de tareas no es válido.");
        }

        return _repositorio.ListarTareasAsync(idUsuario,idEstudiante, filtroNormalizado, cancelacion);
    }

    public Task<IReadOnlyList<PortalPagoResponse>> ListarPagosAsync(int idUsuario,int idEstudiante,string? filtro,CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException("El usuario indicado no es válido.");
        }

        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException( "Seleccione un estudiante válido.");
        }

        var filtroNormalizado = string.IsNullOrWhiteSpace(filtro)
            ? "todos"
            : filtro.Trim().ToLowerInvariant();

        var filtrosPermitidos = new[]
        {
            "pendientes",
            "pagados",
            "todos"
        };

        if (!filtrosPermitidos.Contains(filtroNormalizado))
        {
            throw new ReglaNegocioException( "El filtro de pagos no es válido.");
        }

        return _repositorio.ListarPagosAsync(idUsuario, idEstudiante,filtroNormalizado, cancelacion);
    }

    public Task<PortalPagoDetalleResponse> ObtenerPagoAsync(int idUsuario,int idEstudiante,int idPago,CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException( "El usuario indicado no es válido.");
        }

        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException( "Seleccione un estudiante válido.");
        }

        if (idPago <= 0)
        {
            throw new ReglaNegocioException( "El cargo indicado no es válido.");
        }

        return _repositorio.ObtenerPagoAsync(idUsuario,idEstudiante,idPago,cancelacion);
    }

    public async Task<PortalArchivoTareaResponse> ObtenerArchivoTareaAsync(int idUsuario,int idEstudiante,int idTareaAsignacion,CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException("El usuario indicado no es válido.");
        }

        if (idEstudiante <= 0)
        {
            throw new ReglaNegocioException("Seleccione un estudiante válido.");
        }

        if (idTareaAsignacion <= 0)
        {
            throw new ReglaNegocioException("La asignación indicada no es válida.");
        }

        var archivo = await _repositorio.ObtenerArchivoTareaAsync(idUsuario, idEstudiante, idTareaAsignacion, cancelacion);

        return archivo ?? throw new ReglaNegocioException( "La tarea no tiene un archivo disponible.");
    }

    public async Task<ArchivoDescarga?> AbrirArchivoTareaAsync(int idUsuario, int idEstudiante, int idTareaAsignacion, CancellationToken cancelacion)
    {
        var archivo = await ObtenerArchivoTareaAsync(idUsuario, idEstudiante, idTareaAsignacion, cancelacion);

        return _almacen.AbrirParaDescarga(archivo.RutaArchivo, archivo.Material);
    }
    public async Task<PortalReporteDetalleResponse> ObtenerReporteAsync(int idUsuario, int idEstudiante, int idReporte, CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
            throw new ReglaNegocioException("El usuario indicado no es válido.");

        if (idEstudiante <= 0)
            throw new ReglaNegocioException("Seleccione un estudiante válido.");

        if (idReporte <= 0)
            throw new ReglaNegocioException("El reporte indicado no es válido.");

        var reporte = await _repositorio.ObtenerReporteAsync(idUsuario, idEstudiante, idReporte, cancelacion);

        return reporte ?? throw new ReglaNegocioException("El reporte solicitado no está disponible en el portal.");
    }

    public Task<IReadOnlyList<PortalRecursoResponse>> ListarRecursosAsync(int idUsuario, CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException("El usuario indicado no es válido.");
        }

        return _repositorio.ListarRecursosAsync(idUsuario, cancelacion);
    }

    public async Task<ArchivoDescarga?> ObtenerArchivoRecursoAsync(int idUsuario, int idMaterial, CancellationToken cancelacion)
    {
        var recurso = await ObtenerRecursoAsync(idUsuario, idMaterial, cancelacion);

        if (recurso.IdTipoMaterial != TipoPdf)
        {
            throw new ReglaNegocioException("El recurso no tiene un archivo para descargar.");
        }

        return _almacen.AbrirParaDescarga(recurso.RutaArchivo, recurso.Titulo);
    }

    public async Task<VerificacionEnlaceResponse> VerificarEnlaceRecursoAsync(int idUsuario, int idMaterial, CancellationToken cancelacion)
    {
        var recurso = await ObtenerRecursoAsync(idUsuario, idMaterial, cancelacion);

        if (recurso.IdTipoMaterial != TipoEnlace || recurso.Url is null)
        {
            throw new ReglaNegocioException("El recurso no es un enlace.");
        }

        return await _verificador.VerificarAsync(recurso.Url, cancelacion);
    }

    private async Task<PortalRecursoDetalleResponse> ObtenerRecursoAsync(int idUsuario, int idMaterial, CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException("El usuario indicado no es válido.");
        }

        if (idMaterial <= 0)
        {
            throw new ReglaNegocioException("El recurso no está disponible.");
        }

        var recurso = await _repositorio.ObtenerRecursoAsync(idUsuario, idMaterial, cancelacion);

        return recurso ?? throw new ReglaNegocioException("El recurso no está disponible.");
    }

}