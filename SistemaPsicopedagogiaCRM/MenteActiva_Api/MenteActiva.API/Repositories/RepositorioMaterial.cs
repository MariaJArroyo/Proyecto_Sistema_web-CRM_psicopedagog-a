using Dapper;
using MenteActiva.Api.Models;
using MySqlConnector;

namespace MenteActiva.Api.Repositories;

// El listado va por SP_Material_Listar, que ya existia y lo comparte el resto
// del sistema. Lo demas va con SQL en linea a proposito: el material es una
// tabla plana sin bitacora ni reglas que crucen tablas, y la seccion 6 del
// proyecto pone su CRUD y el publicar/despublicar por consulta directa.
//
// Hereda de RepositorioBase solo por el listado. Las consultas en linea abren
// su propia conexion porque la base solo sabe llamar procedimientos.
//
// Los UPDATE dependen de que MySqlConnector cuente filas encontradas y no
// filas cambiadas (UseAffectedRows=false, que es su valor por omision): asi
// guardar sin cambios devuelve 1 y no se confunde con "no existe".
public sealed class RepositorioMaterial : RepositorioBase, IRepositorioMaterial
{
    private const string SeleccionMaterial =
        "SELECT m.IdMaterial, m.Titulo, m.Descripcion, tm.Nombre AS TipoMaterial, " +
        "       m.Url, m.RutaArchivo, m.Publicado, m.FechaPublicacion, " +
        "       u.NombreCompleto AS RegistradoPor, m.IdTipoMaterial, " +
        "       m.IdNivelEducativo, ne.Nombre AS NivelEducativo, " +
        "       m.TipoActividad, m.FechaActividad, m.Activo " +
        "FROM TB_MATERIAL m " +
        "JOIN TB_TIPO_MATERIAL tm ON tm.IdTipoMaterial = m.IdTipoMaterial " +
        "JOIN TB_USUARIO u ON u.IdUsuario = m.IdUsuarioRegistro " +
        "LEFT JOIN TB_NIVEL_EDUCATIVO ne ON ne.IdNivelEducativo = m.IdNivelEducativo " +
        "WHERE m.IdMaterial = @IdMaterial AND m.Activo = 1;";

    private const string Insercion =
        "INSERT INTO TB_MATERIAL " +
        "(IdTipoMaterial, IdUsuarioRegistro, Titulo, Descripcion, Url, RutaArchivo, " +
        " IdNivelEducativo, TipoActividad, FechaActividad, Publicado, FechaPublicacion) " +
        "VALUES " +
        "(@IdTipoMaterial, @IdUsuarioRegistro, @Titulo, @Descripcion, @Url, @RutaArchivo, " +
        " @IdNivelEducativo, @TipoActividad, @FechaActividad, @Publicar, IF(@Publicar, NOW(), NULL)); " +
        "SELECT LAST_INSERT_ID();";

    private const string Edicion =
        "UPDATE TB_MATERIAL " +
        "SET Titulo = @Titulo, Descripcion = @Descripcion, Url = @Url, " +
        "    IdNivelEducativo = @IdNivelEducativo, TipoActividad = @TipoActividad, " +
        "    FechaActividad = @FechaActividad " +
        "WHERE IdMaterial = @IdMaterial AND Activo = 1;";

    // FechaPublicacion va antes que Publicado a proposito: MySQL asigna de
    // izquierda a derecha y, al reves, el IF leeria el Publicado ya cambiado.
    // Volver a publicar algo ya publicado conserva su fecha.
    private const string Publicacion =
        "UPDATE TB_MATERIAL " +
        "SET FechaPublicacion = IF(@Publicado, IF(Publicado = 1, FechaPublicacion, NOW()), NULL), " +
        "    Publicado = @Publicado " +
        "WHERE IdMaterial = @IdMaterial AND Activo = 1;";

    private const string Baja =
        "UPDATE TB_MATERIAL SET Activo = 0 WHERE IdMaterial = @IdMaterial AND Activo = 1;";

    private const string ExisteNivel =
        "SELECT EXISTS(SELECT 1 FROM TB_NIVEL_EDUCATIVO WHERE IdNivelEducativo = @IdNivelEducativo);";

    private readonly string _cadenaConexion;

    public RepositorioMaterial(IConfiguration config)
        : base(config)
    {
        _cadenaConexion = config.GetConnectionString("DefaultConnection")!;
    }

    // ---------- consultas ----------

    public Task<IReadOnlyList<MaterialResponse>> ListarAsync(
        int? idTipoMaterial, bool? soloPublicados, string? busqueda, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdTipoMaterial", idTipoMaterial);
        parametros.Add("p_SoloPublicados", soloPublicados);
        parametros.Add("p_Busqueda", busqueda);

        return ConsultarAsync<MaterialResponse>("SP_Material_Listar", parametros, cancelacion);
    }

    public async Task<MaterialResponse?> ObtenerAsync(int idMaterial, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        return await conexion.QueryFirstOrDefaultAsync<MaterialResponse>(
            new CommandDefinition(SeleccionMaterial, new { IdMaterial = idMaterial },
                cancellationToken: cancelacion));
    }

    public async Task<bool> ExisteNivelEducativoAsync(
        int idNivelEducativo, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExisteNivel, new { IdNivelEducativo = idNivelEducativo },
                cancellationToken: cancelacion));
    }

    // ---------- escrituras ----------

    public async Task<int> CrearAsync(MaterialGuardar material, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        return await conexion.ExecuteScalarAsync<int>(
            new CommandDefinition(Insercion, material, cancellationToken: cancelacion));
    }

    public Task<bool> EditarAsync(
        int idMaterial, MaterialGuardar material, CancellationToken cancelacion)
        => EjecutarSqlAsync(Edicion, new
        {
            IdMaterial = idMaterial,
            material.Titulo,
            material.Descripcion,
            material.Url,
            material.IdNivelEducativo,
            material.TipoActividad,
            material.FechaActividad
        }, cancelacion);

    public Task<bool> CambiarPublicacionAsync(
        int idMaterial, bool publicado, CancellationToken cancelacion)
        => EjecutarSqlAsync(Publicacion,
            new { IdMaterial = idMaterial, Publicado = publicado }, cancelacion);

    public Task<bool> EliminarAsync(int idMaterial, CancellationToken cancelacion)
        => EjecutarSqlAsync(Baja, new { IdMaterial = idMaterial }, cancelacion);

    private async Task<bool> EjecutarSqlAsync(
        string sql, object parametros, CancellationToken cancelacion)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);

        var filas = await conexion.ExecuteAsync(
            new CommandDefinition(sql, parametros, cancellationToken: cancelacion));

        return filas > 0;
    }
}
