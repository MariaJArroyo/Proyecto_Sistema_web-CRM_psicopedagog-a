using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioPlantillas
{
    Task<IReadOnlyList<PlantillaResponse>> ListarAsync(bool soloActivas, CancellationToken cancelacion);

    Task<PlantillaResponse> ObtenerAsync(int idPlantilla, CancellationToken cancelacion);

    CatalogosPlantillaResponse ObtenerCatalogos();

    Task<int> CrearAsync(
        int idUsuarioAccion, GuardarPlantillaRequest peticion, CancellationToken cancelacion);

    Task EditarAsync(
        int idUsuarioAccion, int idPlantilla, GuardarPlantillaRequest peticion,
        CancellationToken cancelacion);

    Task CambiarEstadoAsync(
        int idUsuarioAccion, int idPlantilla, bool activa, CancellationToken cancelacion);
}
