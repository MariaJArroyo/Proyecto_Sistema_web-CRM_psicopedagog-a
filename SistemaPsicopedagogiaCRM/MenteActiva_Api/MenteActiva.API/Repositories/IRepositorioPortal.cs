using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories

{
    public interface IRepositorioPortal
    {
        Task<PortalResponse> ObtenerInicioAsync(int idUsuario,int? idEstudiante, CancellationToken cancelacion);
    }
}
