using System.Security.Claims;

namespace MenteActiva.Api.Infraestructura;

public static class ClaimsPrincipalExtensions
{
    // Reemplaza el int.Parse(...) repetido en cada controlador
    public static int ObtenerIdUsuario(this ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(valor, out var idUsuario)
            ? idUsuario
            : throw new InvalidOperationException("El token no trae el identificador del usuario.");
    }
}