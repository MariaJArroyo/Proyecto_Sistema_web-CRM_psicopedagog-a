using System.Net.Http.Headers;

namespace MenteActiva.Seguridad;

// Agrega el token a cada llamada que la Web le hace al API. Va aqui y no en los
// controladores para que nadie tenga que acordarse de ponerlo, y para que el
// token no ande suelto por el codigo de las pantallas.
public class ManejadorTokenApi : DelegatingHandler
{
    private readonly IHttpContextAccessor _contexto;

    public ManejadorTokenApi(IHttpContextAccessor contexto)
    {
        _contexto = contexto;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage solicitud,
        CancellationToken cancelacion)
    {
        var token = _contexto.HttpContext?.User?.FindFirst(ClaimsMenteActiva.TokenApi)?.Value;

        // Si no hay sesion, la peticion sale sin token y el API responde 401.
        // Eso es lo correcto: la Web no inventa credenciales.
        if (!string.IsNullOrWhiteSpace(token) && solicitud.Headers.Authorization is null)
        {
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(solicitud, cancelacion);
    }
}
