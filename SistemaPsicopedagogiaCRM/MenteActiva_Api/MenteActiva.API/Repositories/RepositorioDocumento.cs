using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioDocumento
{
    Task<int> CrearAsync(
        int idUsuarioAccion, int idEstudiante, string titulo, string? descripcion,
        string archivoNombre, string nombreOriginal, string tipo, long tamano, bool visible,
        CancellationToken cancelacion);

    Task<IReadOnlyList<DocumentoEstudianteResponse>> ListarPorEstudianteAsync(
        int idEstudiante, CancellationToken cancelacion);

    Task<DocumentoArchivoRegistro?> ObtenerArchivoAsync(int idDocumento, CancellationToken cancelacion);

    Task CambiarVisibilidadAsync(int idUsuarioAccion, int idDocumento, bool visible, CancellationToken cancelacion);

    Task EliminarAsync(int idUsuarioAccion, int idDocumento, CancellationToken cancelacion);

    Task<IReadOnlyList<PortalDocumentoResponse>> ListarPortalAsync(
        int idUsuario, int idEstudiante, CancellationToken cancelacion);

    Task<DocumentoArchivoRegistro?> ObtenerArchivoPortalAsync(
        int idUsuario, int idEstudiante, int idDocumento, CancellationToken cancelacion);
}

public sealed class RepositorioDocumento : RepositorioBase, IRepositorioDocumento
{
    public RepositorioDocumento(IConfiguration config)
        : base(config)
    {
    }

    public Task<int> CrearAsync(
        int idUsuarioAccion, int idEstudiante, string titulo, string? descripcion,
        string archivoNombre, string nombreOriginal, string tipo, long tamano, bool visible,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Titulo", titulo);
        parametros.Add("p_Descripcion", descripcion);
        parametros.Add("p_ArchivoNombre", archivoNombre);
        parametros.Add("p_NombreOriginal", nombreOriginal);
        parametros.Add("p_TipoArchivo", tipo);
        parametros.Add("p_TamanoBytes", tamano);
        parametros.Add("p_VisibleEnPortal", visible);

        return EscalarAsync("SP_Documento_Crear", parametros, cancelacion);
    }

    public Task<IReadOnlyList<DocumentoEstudianteResponse>> ListarPorEstudianteAsync(
        int idEstudiante, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", idEstudiante);

        return ConsultarAsync<DocumentoEstudianteResponse>(
            "SP_Documento_ListarPorEstudiante", parametros, cancelacion);
    }

    public async Task<DocumentoArchivoRegistro?> ObtenerArchivoAsync(int idDocumento, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdDocumento", idDocumento);

        var filas = await ConsultarAsync<DocumentoArchivoRegistro>(
            "SP_Documento_ObtenerArchivo", parametros, cancelacion);

        return filas.SingleOrDefault();
    }

    public Task CambiarVisibilidadAsync(
        int idUsuarioAccion, int idDocumento, bool visible, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdDocumento", idDocumento);
        parametros.Add("p_Visible", visible);

        return EjecutarAsync("SP_Documento_CambiarVisibilidad", parametros, cancelacion);
    }

    public Task EliminarAsync(int idUsuarioAccion, int idDocumento, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdDocumento", idDocumento);

        return EjecutarAsync("SP_Documento_Eliminar", parametros, cancelacion);
    }

    public Task<IReadOnlyList<PortalDocumentoResponse>> ListarPortalAsync(
        int idUsuario, int idEstudiante, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);

        return ConsultarAsync<PortalDocumentoResponse>("SP_Portal_ListarDocumentos", parametros, cancelacion);
    }

    public async Task<DocumentoArchivoRegistro?> ObtenerArchivoPortalAsync(
        int idUsuario, int idEstudiante, int idDocumento, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_IdDocumento", idDocumento);

        var filas = await ConsultarAsync<DocumentoArchivoRegistro>(
            "SP_Portal_ObtenerDocumento", parametros, cancelacion);

        return filas.SingleOrDefault();
    }
}
