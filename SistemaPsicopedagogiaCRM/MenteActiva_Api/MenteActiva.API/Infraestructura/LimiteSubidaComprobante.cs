using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MenteActiva.Api.Services;

namespace MenteActiva.Api.Infraestructura;

// Lo mismo que LimiteSubidaPdf, para el comprobante de pago que sube la
// familia: corta antes de leer el formulario si la peticion ya declara que
// es mas grande que el maximo, y le pone ese tope al lector del multipart.
// El tamanio exacto del archivo lo revisa ServicioReportesPago.
public sealed class LimiteSubidaComprobante : IResourceFilter
{
    private const long MargenFormulario = 64 * 1024;

    public void OnResourceExecuting(ResourceExecutingContext contexto)
    {
        var http = contexto.HttpContext;
        var tope = AlmacenComprobantes.TamanoMaximoBytes + MargenFormulario;

        if (http.Request.ContentLength > tope)
        {
            contexto.Result = new BadRequestObjectResult(new
            {
                mensaje = $"El comprobante no puede pasar de {AlmacenComprobantes.TamanoMaximoMb} MB."
            });
            return;
        }

        var limiteCuerpo = http.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (limiteCuerpo is { IsReadOnly: false })
        {
            limiteCuerpo.MaxRequestBodySize = tope;
        }

        if (http.Features.Get<IFormFeature>()?.Form is null)
        {
            http.Features.Set<IFormFeature>(new FormFeature(http.Request, new FormOptions
            {
                MultipartBodyLengthLimit = tope
            }));
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext contexto)
    {
    }
}
