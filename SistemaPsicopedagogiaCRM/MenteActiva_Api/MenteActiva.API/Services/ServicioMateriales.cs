using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;
using Microsoft.Extensions.Options;

namespace MenteActiva.Api.Services;

// Aqui viven las reglas del material, porque se escribe por consulta directa y
// no hay procedimiento que las cuide. La tabla tiene CHECK que repiten las
// mismas reglas (tipo y fecha solo en la actividad, url en el enlace, archivo
// en el PDF); se revisan antes para que la persona reciba un 400 con un
// mensaje y no un 500 por una restriccion violada.
public sealed class ServicioMateriales : IServicioMateriales
{
    // Ids fijos de TB_TIPO_MATERIAL, sembrados en 02_catalogos.sql
    private const int TipoGuia = 1;
    private const int TipoPdf = 2;
    private const int TipoEnlace = 3;
    private const int TipoActividad = 4;

    private const int LargoBusqueda = 150;
    private const int LargoTitulo = 150;

    // Los valores que acepta CK_Material_TipoActividad
    private static readonly string[] TiposActividad = { "Ludica", "Cognitiva", "Motriz" };

    private readonly IRepositorioMaterial _repositorio;
    private readonly IAlmacenMateriales _almacen;
    private readonly IVerificadorEnlaces _verificador;
    private readonly OpcionesMateriales _opciones;

    public ServicioMateriales(
        IRepositorioMaterial repositorio,
        IAlmacenMateriales almacen,
        IVerificadorEnlaces verificador,
        IOptions<OpcionesMateriales> opciones)
    {
        _repositorio = repositorio;
        _almacen = almacen;
        _verificador = verificador;
        _opciones = opciones.Value;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    public Task<IReadOnlyList<MaterialResponse>> ListarAsync(
        int? idTipoMaterial, bool? soloPublicados, string? busqueda, CancellationToken cancelacion)
    {
        busqueda = Limpiar(busqueda);

        if (busqueda is { Length: > LargoBusqueda })
        {
            busqueda = busqueda[..LargoBusqueda];
        }

        return _repositorio.ListarAsync(idTipoMaterial, soloPublicados, busqueda, cancelacion);
    }

    public Task<MaterialResponse?> ObtenerAsync(int idMaterial, CancellationToken cancelacion)
    {
        if (idMaterial <= 0)
        {
            return Task.FromResult<MaterialResponse?>(null);
        }

        return _repositorio.ObtenerAsync(idMaterial, cancelacion);
    }

    // Nulo si el material no existe, esta dado de baja o su archivo ya no esta
    // en disco: el controlador responde 404 en los tres casos. Sirve aunque el
    // material sea borrador, porque es la descarga del personal.
    public async Task<ArchivoDescarga?> ObtenerArchivoAsync(int idMaterial, CancellationToken cancelacion)
    {
        var material = await ObtenerAsync(idMaterial, cancelacion);

        if (material is null)
        {
            return null;
        }

        if (material.IdTipoMaterial != TipoPdf)
        {
            throw new ReglaNegocioException("El material no es un PDF.");
        }

        return _almacen.AbrirParaDescarga(material.RutaArchivo, material.Titulo);
    }

    // Nulo si no existe o esta dado de baja
    public async Task<VerificacionEnlaceResponse?> VerificarEnlaceAsync(
        int idMaterial, CancellationToken cancelacion)
    {
        var material = await ObtenerAsync(idMaterial, cancelacion);

        if (material is null)
        {
            return null;
        }

        if (material.IdTipoMaterial != TipoEnlace || material.Url is null)
        {
            throw new ReglaNegocioException("El material no es un enlace.");
        }

        return await _verificador.VerificarAsync(material.Url, cancelacion);
    }

    // =====================================================
    // ALTAS
    // =====================================================

    // Todo lo que se puede revisar sin tocar el disco va primero, para no
    // escribir un archivo que despues haya que borrar. Si la insercion falla,
    // el archivo se borra: no quedan PDF sin fila.
    public async Task<int> CrearPdfAsync(
        int idUsuarioAccion, CrearPdfRequest peticion, CancellationToken cancelacion)
    {
        var archivo = peticion.Archivo;

        if (archivo is null || archivo.Length == 0)
        {
            throw new ReglaNegocioException("Seleccione un archivo PDF.");
        }

        if (!string.Equals(Path.GetExtension(archivo.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReglaNegocioException("Solo se permiten archivos en formato PDF.");
        }

        if (archivo.Length > _opciones.TamanoMaximoBytes)
        {
            throw new ReglaNegocioException(
                $"El archivo no puede pasar de {_opciones.TamanoMaximoMb} MB.");
        }

        var titulo = Limpiar(peticion.Titulo) ?? TituloDesdeArchivo(archivo.FileName);

        await ValidarNivelAsync(peticion.IdNivelEducativo, obligatorio: false, cancelacion);

        string nombreEnDisco;

        await using (var contenido = archivo.OpenReadStream())
        {
            nombreEnDisco = await _almacen.GuardarPdfAsync(contenido, cancelacion);
        }

        try
        {
            // Sin el token: con el archivo ya escrito, cortar a medias la
            // insercion dejaria la duda de si la fila quedo o no
            return await _repositorio.CrearAsync(new MaterialGuardar(
                TipoPdf, idUsuarioAccion, titulo, Limpiar(peticion.Descripcion),
                Url: null, nombreEnDisco, peticion.IdNivelEducativo,
                TipoActividad: null, FechaActividad: null, peticion.Publicar), CancellationToken.None);
        }
        catch
        {
            _almacen.Borrar(nombreEnDisco);
            throw;
        }
    }

    public async Task<int> CrearGuiaAsync(
        int idUsuarioAccion, CrearGuiaRequest peticion, CancellationToken cancelacion)
    {
        await ValidarNivelAsync(peticion.IdNivelEducativo, obligatorio: true, cancelacion);

        return await _repositorio.CrearAsync(new MaterialGuardar(
            TipoGuia, idUsuarioAccion, Titulo(peticion), Limpiar(peticion.Descripcion),
            Url: null, RutaArchivo: null, peticion.IdNivelEducativo,
            TipoActividad: null, FechaActividad: null, peticion.Publicar), cancelacion);
    }

    public async Task<int> CrearEnlaceAsync(
        int idUsuarioAccion, CrearEnlaceRequest peticion, CancellationToken cancelacion)
    {
        var url = ValidarUrl(peticion.Url);

        await ValidarNivelAsync(peticion.IdNivelEducativo, obligatorio: false, cancelacion);

        return await _repositorio.CrearAsync(new MaterialGuardar(
            TipoEnlace, idUsuarioAccion, Titulo(peticion), Limpiar(peticion.Descripcion),
            url, RutaArchivo: null, peticion.IdNivelEducativo,
            TipoActividad: null, FechaActividad: null, peticion.Publicar), cancelacion);
    }

    public async Task<int> CrearActividadAsync(
        int idUsuarioAccion, CrearActividadRequest peticion, CancellationToken cancelacion)
    {
        var tipoActividad = ValidarTipoActividad(peticion.TipoActividad);
        var fecha = ValidarFechaActividad(peticion.FechaActividad);

        await ValidarNivelAsync(peticion.IdNivelEducativo, obligatorio: false, cancelacion);

        return await _repositorio.CrearAsync(new MaterialGuardar(
            TipoActividad, idUsuarioAccion, Titulo(peticion), Limpiar(peticion.Descripcion),
            Url: null, RutaArchivo: null, peticion.IdNivelEducativo,
            tipoActividad, fecha, peticion.Publicar), cancelacion);
    }

    // =====================================================
    // CAMBIOS
    // =====================================================

    // Se lee primero el material para saber su tipo: de eso depende que campos
    // de la peticion se usan. Los que no le tocan al tipo se ignoran y se deja
    // lo que ya tenia, para que la pantalla pueda mandar siempre la misma forma.
    public async Task EditarAsync(
        int idMaterial, EditarMaterialRequest peticion, CancellationToken cancelacion)
    {
        var actual = await ObtenerAsync(idMaterial, cancelacion)
            ?? throw NoExiste();

        var url = actual.Url;
        string? tipoActividad = null;
        DateTime? fecha = null;

        switch (actual.IdTipoMaterial)
        {
            case TipoEnlace:
                url = ValidarUrl(peticion.Url);
                break;

            case TipoActividad:
                tipoActividad = ValidarTipoActividad(peticion.TipoActividad);
                fecha = ValidarFechaActividad(peticion.FechaActividad);
                break;
        }

        await ValidarNivelAsync(
            peticion.IdNivelEducativo, obligatorio: actual.IdTipoMaterial == TipoGuia, cancelacion);

        var guardado = await _repositorio.EditarAsync(idMaterial, new MaterialGuardar(
            actual.IdTipoMaterial, IdUsuarioRegistro: 0, Titulo(peticion),
            Limpiar(peticion.Descripcion), url, RutaArchivo: null, peticion.IdNivelEducativo,
            tipoActividad, fecha, Publicar: false), cancelacion);

        // Lo dieron de baja entre la lectura y la escritura
        if (!guardado)
        {
            throw NoExiste();
        }
    }

    // Publicar lo que ya esta publicado no es error: conserva su fecha
    public async Task CambiarPublicacionAsync(
        int idMaterial, bool publicado, CancellationToken cancelacion)
    {
        ValidarId(idMaterial);

        if (!await _repositorio.CambiarPublicacionAsync(idMaterial, publicado, cancelacion))
        {
            throw NoExiste();
        }
    }

    // Baja logica (RG-97). El archivo de un PDF se queda en disco: el material
    // sigue en la tabla como historial y la fila apunta a el.
    public async Task EliminarAsync(int idMaterial, CancellationToken cancelacion)
    {
        ValidarId(idMaterial);

        if (!await _repositorio.EliminarAsync(idMaterial, cancelacion))
        {
            throw NoExiste();
        }
    }

    // =====================================================
    // VALIDACIONES
    // =====================================================

    private async Task ValidarNivelAsync(
        int? idNivelEducativo, bool obligatorio, CancellationToken cancelacion)
    {
        if (idNivelEducativo is null)
        {
            if (obligatorio)
            {
                throw new ReglaNegocioException("Seleccione el nivel educativo.");
            }

            return;
        }

        if (idNivelEducativo <= 0
            || !await _repositorio.ExisteNivelEducativoAsync(idNivelEducativo.Value, cancelacion))
        {
            throw new ReglaNegocioException("El nivel educativo indicado no existe.");
        }
    }

    // Solo formato: http o https y con servidor. Si el enlace responde o no se
    // comprueba al abrirlo, no al guardarlo.
    private static string ValidarUrl(string? valor)
    {
        var url = Limpiar(valor);

        if (url is null)
        {
            throw new ReglaNegocioException("Indique la dirección del enlace.");
        }

        if (url.Length > 500)
        {
            throw new ReglaNegocioException("La dirección no puede pasar de 500 caracteres.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host))
        {
            throw new ReglaNegocioException(
                "La dirección del enlace debe empezar con http:// o https:// y ser válida.");
        }

        return url;
    }

    // Devuelve el valor con la forma exacta que acepta el CHECK, aunque llegue
    // en otra combinacion de mayusculas
    private static string ValidarTipoActividad(string? valor)
    {
        var limpio = Limpiar(valor);

        return TiposActividad.FirstOrDefault(
                t => string.Equals(t, limpio, StringComparison.OrdinalIgnoreCase))
            ?? throw new ReglaNegocioException(
                "El tipo de actividad debe ser Lúdica, Cognitiva o Motriz.");
    }

    private static DateTime ValidarFechaActividad(DateTime? fecha)
        => fecha?.Date ?? throw new ReglaNegocioException("Indique la fecha de la actividad.");

    private static string Titulo(MaterialBaseRequest peticion)
        => Limpiar(peticion.Titulo) ?? throw new ReglaNegocioException("Indique el título.");

    // "Guia de fracciones.pdf" -> "Guia de fracciones". Lo que manda el
    // navegador puede traer ruta en algunos sistemas, por eso GetFileName.
    private static string TituloDesdeArchivo(string nombreArchivo)
    {
        var titulo = Limpiar(Path.GetFileNameWithoutExtension(Path.GetFileName(nombreArchivo)))
            ?? throw new ReglaNegocioException("Indique el título.");

        return titulo.Length > LargoTitulo ? titulo[..LargoTitulo].TrimEnd() : titulo;
    }

    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static void ValidarId(int idMaterial)
    {
        if (idMaterial <= 0)
        {
            throw NoExiste();
        }
    }

    private static ReglaNegocioException NoExiste()
        => new("El material indicado no existe.");
}
