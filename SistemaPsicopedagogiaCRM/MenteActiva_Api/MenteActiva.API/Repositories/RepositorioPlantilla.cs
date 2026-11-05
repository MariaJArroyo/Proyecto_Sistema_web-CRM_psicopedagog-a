using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

// Lo unico que habla con la base. Aqui no hay reglas: las de datos estan en los
// procedimientos y las de texto en ServicioPlantillas.
public sealed class RepositorioPlantilla : RepositorioBase, IRepositorioPlantilla
{
    public RepositorioPlantilla(IConfiguration config)
        : base(config)
    {
    }

    // ---------- consultas ----------

    public Task<IReadOnlyList<PlantillaResponse>> ListarAsync(
        bool soloActivas, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_SoloActivas", soloActivas);

        return ConsultarAsync<PlantillaResponse>("SP_Plantilla_Listar", parametros, cancelacion);
    }

    public async Task<PlantillaResponse?> ObtenerAsync(int idPlantilla, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdPlantilla", idPlantilla);

        var filas = await ConsultarAsync<PlantillaResponse>(
            "SP_Plantilla_ObtenerDetalle", parametros, cancelacion);

        return filas.FirstOrDefault();
    }

    public async Task<PlantillaResponse?> ObtenerPorClaveAsync(
        string clave, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_Clave", clave);

        var filas = await ConsultarAsync<PlantillaResponse>(
            "SP_Plantilla_ObtenerPorClave", parametros, cancelacion);

        return filas.FirstOrDefault();
    }

    // ---------- escrituras ----------

    public Task<int> GuardarAsync(
        int idUsuarioAccion, int? idPlantilla, GuardarPlantillaRequest peticion,
        CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdPlantilla", idPlantilla);
        parametros.Add("p_Nombre", peticion.Nombre.Trim());
        parametros.Add("p_Asunto", peticion.Asunto?.Trim());
        parametros.Add("p_Cuerpo", peticion.Cuerpo.Trim());

        // El procedimiento cierra con SELECT del id, creando o editando
        return EscalarAsync("SP_Plantilla_Guardar", parametros, cancelacion);
    }

    public Task CambiarEstadoAsync(
        int idUsuarioAccion, int idPlantilla, bool activa, CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuarioAccion", idUsuarioAccion);
        parametros.Add("p_IdPlantilla", idPlantilla);
        parametros.Add("p_Activa", activa);

        return EjecutarAsync("SP_Plantilla_CambiarEstado", parametros, cancelacion);
    }
}
