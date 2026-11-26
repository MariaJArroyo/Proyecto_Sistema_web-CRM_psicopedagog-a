using Dapper;

namespace MenteActiva.Api.Repositories;

public sealed class FotoEstudianteRegistro
{
    public string? FotoArchivo { get; set; }

    public string? FotoTipo { get; set; }
}

public interface IRepositorioFotoEstudiante
{
    Task<FotoEstudianteRegistro?> ObtenerAsync(int idEstudiante, CancellationToken cancelacion);

    // archivo nulo = quitar la foto. Devuelve el archivo que habia antes.
    Task<string?> GuardarAsync(int idEstudiante, string? archivo, string? tipo, CancellationToken cancelacion);
}

public sealed class RepositorioFotoEstudiante : RepositorioBase, IRepositorioFotoEstudiante
{
    public RepositorioFotoEstudiante(IConfiguration config)
        : base(config)
    {
    }

    public async Task<FotoEstudianteRegistro?> ObtenerAsync(int idEstudiante, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", idEstudiante);

        var filas = await ConsultarAsync<FotoEstudianteRegistro>("SP_Estudiante_ObtenerFoto", parametros, cancelacion);

        return filas.FirstOrDefault();
    }

    public async Task<string?> GuardarAsync(
        int idEstudiante, string? archivo, string? tipo, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();
        parametros.Add("p_IdEstudiante", idEstudiante);
        parametros.Add("p_Archivo", archivo);
        parametros.Add("p_Tipo", tipo);

        var filas = await ConsultarAsync<string?>("SP_Estudiante_GuardarFoto", parametros, cancelacion);

        return filas.FirstOrDefault();
    }
}
