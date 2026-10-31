using Dapper;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Repositories;

public class RepositorioPortal : RepositorioBase, IRepositorioPortal
{
    public RepositorioPortal(IConfiguration config)
        : base(config)
    {
    }

    public Task<PortalResponse> ObtenerInicioAsync(
        int idUsuario,int? idEstudiante,CancellationToken cancelacion)
    {
        var parametros = new DynamicParameters();

        parametros.Add("p_IdUsuario", idUsuario);
        parametros.Add("p_IdEstudiante", idEstudiante);

        return ConsultarVariosAsync<PortalResponse>(
            "SP_Portal_ObtenerInicio",parametros,
            async lector =>
            {
                // Primer resultado: estudiantes del encargado.
                var estudiantes = (await lector.ReadAsync<PortalEstudianteResponse>()).AsList();

                // Segundo resultado: resumen del estudiante seleccionado.
                var resumen = await lector.ReadSingleAsync<PortalResumenResponse>();

                return new PortalResponse
                {
                    Estudiantes = estudiantes,
                    Resumen = resumen
                };
            },
            cancelacion);
    }
}