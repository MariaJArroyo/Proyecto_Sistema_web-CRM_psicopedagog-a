using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IServicioPortal
{
    Task<PortalResponse> ObtenerInicioAsync(int idUsuario,int? idEstudiante, CancellationToken cancelacion);
}