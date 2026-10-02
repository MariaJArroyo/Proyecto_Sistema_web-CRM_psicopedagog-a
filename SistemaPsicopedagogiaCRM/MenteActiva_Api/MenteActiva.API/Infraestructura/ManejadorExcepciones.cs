using Microsoft.AspNetCore.Diagnostics;

namespace MenteActiva.Api.Infraestructura;

// Un solo lugar que convierte excepciones en respuestas HTTP, para que los
// controladores no repitan try/catch. Responde { mensaje }, el formato que ya
// lee la Web.
public sealed class ManejadorExcepciones : IExceptionHandler
{
    private readonly ILogger<ManejadorExcepciones> _registro;

    public ManejadorExcepciones(ILogger<ManejadorExcepciones> registro)
    {
        _registro = registro;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto,
        Exception excepcion,
        CancellationToken cancelacion)
    {
        if (excepcion is ReglaNegocioException regla)
        {
            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            await contexto.Response.WriteAsJsonAsync(new { mensaje = regla.Message }, cancelacion);
            return true;
        }

        // El cliente cerro la peticion: no hay a quien responder ni nada que registrar
        if (excepcion is OperationCanceledException && contexto.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        _registro.LogError(excepcion, "Error no controlado en {Metodo} {Ruta}",
            contexto.Request.Method, contexto.Request.Path);

        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await contexto.Response.WriteAsJsonAsync(
            new { mensaje = "Ocurrió un error inesperado. Intente de nuevo." }, cancelacion);

        return true;
    }
}