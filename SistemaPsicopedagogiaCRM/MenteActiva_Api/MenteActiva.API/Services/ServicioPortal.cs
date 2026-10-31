using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

public class ServicioPortal : IServicioPortal
{
    private readonly IRepositorioPortal _repositorio;

    public ServicioPortal(IRepositorioPortal repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<PortalResponse> ObtenerInicioAsync(int idUsuario,int? idEstudiante,CancellationToken cancelacion)
    {
        if (idUsuario <= 0)
        {
            throw new ReglaNegocioException("El usuario indicado no es válido.");
        }

        if (idEstudiante.HasValue && idEstudiante.Value <= 0)
        {
            throw new ReglaNegocioException("El estudiante indicado no es válido.");
        }

        return _repositorio.ObtenerInicioAsync(idUsuario, idEstudiante,cancelacion);
    }
}