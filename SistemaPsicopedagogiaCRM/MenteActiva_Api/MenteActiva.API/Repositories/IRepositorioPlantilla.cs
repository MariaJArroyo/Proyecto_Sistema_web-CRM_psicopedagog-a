using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public interface IRepositorioPlantilla
{
    // ---------- consultas ----------

    Task<IReadOnlyList<PlantillaResponse>> ListarAsync(bool soloActivas, CancellationToken cancelacion);

    Task<PlantillaResponse?> ObtenerAsync(int idPlantilla, CancellationToken cancelacion);

    // Para el envio de la fase 2 y para el recordatorio automatico: la plantilla
    // del sistema se busca por su clave, no por id ni por nombre.
    Task<PlantillaResponse?> ObtenerPorClaveAsync(string clave, CancellationToken cancelacion);

    // ---------- escrituras ----------

    // Crea si idPlantilla viene nulo y edita si trae valor. Devuelve el id.
    Task<int> GuardarAsync(
        int idUsuarioAccion, int? idPlantilla, GuardarPlantillaRequest peticion,
        CancellationToken cancelacion);

    Task CambiarEstadoAsync(
        int idUsuarioAccion, int idPlantilla, bool activa, CancellationToken cancelacion);
}
