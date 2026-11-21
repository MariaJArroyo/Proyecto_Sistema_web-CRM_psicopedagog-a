using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioMateriales
{
    Task<IReadOnlyList<MaterialResponse>> ListarAsync(
        int? idTipoMaterial, bool? soloPublicados, string? busqueda, CancellationToken cancelacion);

    // Nulo cuando no existe o esta dado de baja, para que el controlador
    // responda 404
    Task<MaterialResponse?> ObtenerAsync(int idMaterial, CancellationToken cancelacion);

    // Los tres devuelven el id creado
    Task<int> CrearGuiaAsync(int idUsuarioAccion, CrearGuiaRequest peticion, CancellationToken cancelacion);

    Task<int> CrearEnlaceAsync(int idUsuarioAccion, CrearEnlaceRequest peticion, CancellationToken cancelacion);

    Task<int> CrearActividadAsync(int idUsuarioAccion, CrearActividadRequest peticion, CancellationToken cancelacion);

    // Valida extension, tamanio maximo y que el contenido empiece con %PDF.
    // Guarda el archivo y luego la fila; si la fila falla, borra el archivo.
    Task<int> CrearPdfAsync(int idUsuarioAccion, CrearPdfRequest peticion, CancellationToken cancelacion);

    // Nulo cuando el material o su archivo no estan; ReglaNegocioException si
    // el material no es un PDF. Quien llama cierra el flujo (File() lo hace).
    Task<ArchivoDescarga?> ObtenerArchivoAsync(int idMaterial, CancellationToken cancelacion);

    // Nulo cuando el material no existe; ReglaNegocioException si no es enlace
    Task<VerificacionEnlaceResponse?> VerificarEnlaceAsync(int idMaterial, CancellationToken cancelacion);

    Task EditarAsync(int idMaterial, EditarMaterialRequest peticion, CancellationToken cancelacion);

    Task CambiarPublicacionAsync(int idMaterial, bool publicado, CancellationToken cancelacion);

    Task EliminarAsync(int idMaterial, CancellationToken cancelacion);
}
