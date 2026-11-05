using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Las reglas de datos (nombre repetido, plantilla del sistema que no se apaga)
// viven en los procedimientos, que es el unico lugar por donde se escribe. Aqui
// vive lo que es validacion de texto y en SQL seria pelearse con REGEXP: que
// los marcadores entre corchetes sean de la lista cerrada.
public sealed class ServicioPlantillas : IServicioPlantillas
{
    private readonly IRepositorioPlantilla _repositorio;

    public ServicioPlantillas(IRepositorioPlantilla repositorio)
    {
        _repositorio = repositorio;
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    public Task<IReadOnlyList<PlantillaResponse>> ListarAsync(
        bool soloActivas, CancellationToken cancelacion)
        => _repositorio.ListarAsync(soloActivas, cancelacion);

    public async Task<PlantillaResponse> ObtenerAsync(int idPlantilla, CancellationToken cancelacion)
    {
        if (idPlantilla <= 0)
        {
            throw new ReglaNegocioException("La plantilla indicada no es válida.");
        }

        var plantilla = await _repositorio.ObtenerAsync(idPlantilla, cancelacion);

        return plantilla ?? throw new ReglaNegocioException("La plantilla indicada no existe.");
    }

    public CatalogosPlantillaResponse ObtenerCatalogos()
        => new() { Marcadores = MarcadoresPlantilla.ParaMostrar() };

    // =====================================================
    // ESCRITURAS
    // =====================================================

    public Task<int> CrearAsync(
        int idUsuarioAccion, GuardarPlantillaRequest peticion, CancellationToken cancelacion)
    {
        ValidarMarcadores(peticion);

        return _repositorio.GuardarAsync(idUsuarioAccion, null, peticion, cancelacion);
    }

    public async Task EditarAsync(
        int idUsuarioAccion, int idPlantilla, GuardarPlantillaRequest peticion,
        CancellationToken cancelacion)
    {
        if (idPlantilla <= 0)
        {
            throw new ReglaNegocioException("La plantilla indicada no es válida.");
        }

        ValidarMarcadores(peticion);

        await _repositorio.GuardarAsync(idUsuarioAccion, idPlantilla, peticion, cancelacion);
    }

    public Task CambiarEstadoAsync(
        int idUsuarioAccion, int idPlantilla, bool activa, CancellationToken cancelacion)
    {
        if (idPlantilla <= 0)
        {
            throw new ReglaNegocioException("La plantilla indicada no es válida.");
        }

        return _repositorio.CambiarEstadoAsync(idUsuarioAccion, idPlantilla, activa, cancelacion);
    }

    // =====================================================

    // Se revisan el asunto y el cuerpo, no solo el cuerpo: el asunto tambien se
    // sustituye al enviar, y un marcador mal escrito ahi sale igual de mal.
    private static void ValidarMarcadores(GuardarPlantillaRequest peticion)
    {
        var malos = MarcadoresPlantilla.NoPermitidos(peticion.Asunto)
            .Concat(MarcadoresPlantilla.NoPermitidos(peticion.Cuerpo))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (malos.Count == 0)
        {
            return;
        }

        throw new ReglaNegocioException(
            $"Estos marcadores no existen: {MarcadoresPlantilla.Enumerar(malos)}. "
            + $"Los que se pueden usar son: {string.Join(", ", MarcadoresPlantilla.ParaMostrar())}.");
    }
}
