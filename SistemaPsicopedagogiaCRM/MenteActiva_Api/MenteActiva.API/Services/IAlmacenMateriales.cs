namespace MenteActiva.Api.Services;

// Los PDF de materiales en disco, en Archivos/Materiales dentro del
// ContentRootPath del API (la misma carpeta que lee la descarga de tareas del
// portal). En la base solo se guarda el nombre del archivo, nunca una ruta.
public interface IAlmacenMateriales
{
    // Revisa que el contenido empiece con %PDF, lo guarda con un nombre GUID y
    // devuelve ese nombre. Lanza ReglaNegocioException si no es PDF. Si falla
    // a medio escribir, no deja el archivo.
    Task<string> GuardarPdfAsync(Stream contenido, CancellationToken cancelacion);

    // Nulo si el nombre no es un nombre de archivo simple (trae carpetas, ..,
    // dos puntos), si no existe o si es un enlace simbolico. El nombre de
    // descarga sale del titulo, no del nombre en disco.
    ArchivoDescarga? AbrirParaDescarga(string? nombreArchivo, string titulo);

    // Para limpiar cuando la insercion en la base falla. No lanza.
    void Borrar(string nombreArchivo);
}

public sealed record ArchivoDescarga(Stream Contenido, string NombreDescarga);
