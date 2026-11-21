using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioMaterial
{
    // ---------- consultas ----------

    // SP_Material_Listar. Solo activos; los tres filtros admiten nulo.
    Task<IReadOnlyList<MaterialResponse>> ListarAsync(
        int? idTipoMaterial, bool? soloPublicados, string? busqueda, CancellationToken cancelacion);

    // Nulo si no existe o esta dado de baja
    Task<MaterialResponse?> ObtenerAsync(int idMaterial, CancellationToken cancelacion);

    Task<bool> ExisteNivelEducativoAsync(int idNivelEducativo, CancellationToken cancelacion);

    // ---------- escrituras ----------

    // Devuelve el id creado
    Task<int> CrearAsync(MaterialGuardar material, CancellationToken cancelacion);

    // Los tres devuelven false cuando el material no existe o ya esta dado de baja
    Task<bool> EditarAsync(int idMaterial, MaterialGuardar material, CancellationToken cancelacion);

    Task<bool> CambiarPublicacionAsync(int idMaterial, bool publicado, CancellationToken cancelacion);

    Task<bool> EliminarAsync(int idMaterial, CancellationToken cancelacion);
}

// Lo que se escribe en TB_MATERIAL, ya validado y limpio por el servicio.
// IdTipoMaterial, IdUsuarioRegistro, RutaArchivo y Publicar solo se leen al
// crear: editar no cambia el tipo, el autor ni el archivo.
public sealed record MaterialGuardar(
    int IdTipoMaterial,
    int IdUsuarioRegistro,
    string Titulo,
    string? Descripcion,
    string? Url,
    string? RutaArchivo,
    int? IdNivelEducativo,
    string? TipoActividad,
    DateTime? FechaActividad,
    bool Publicar);
