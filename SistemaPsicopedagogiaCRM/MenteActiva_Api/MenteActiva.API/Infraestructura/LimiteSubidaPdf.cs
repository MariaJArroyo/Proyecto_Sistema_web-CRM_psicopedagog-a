using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace MenteActiva.Api.Infraestructura;

// Limite de tamanio solo para la subida de PDF. No se usa [RequestSizeLimit]
// porque ese atributo pide una constante y el maximo vive en appsettings.
//
// Corre antes del enlace de modelo, que es quien lee el formulario: si la
// peticion declara un tamanio mayor se corta aqui con un 400 en espaniol y el
// cuerpo ni se lee. Si no lo declara (envio por partes), Kestrel corta al
// pasarse del tope y el 400 sale del ModelState.
//
// El tope lleva un margen sobre el maximo del archivo porque el multipart
// suma encabezados y los demas campos. El tamanio exacto del archivo lo
// revisa ServicioMateriales.
public sealed class LimiteSubidaPdf : IResourceFilter
{
    private const long MargenFormulario = 64 * 1024;

    private readonly OpcionesMateriales _opciones;

    public LimiteSubidaPdf(IOptions<OpcionesMateriales> opciones)
    {
        _opciones = opciones.Value;
    }

    public void OnResourceExecuting(ResourceExecutingContext contexto)
    {
        var http = contexto.HttpContext;
        var tope = _opciones.TamanoMaximoBytes + MargenFormulario;

        if (http.Request.ContentLength > tope)
        {
            contexto.Result = new BadRequestObjectResult(new
            {
                mensaje = $"El archivo no puede pasar de {_opciones.TamanoMaximoMb} MB."
            });
            return;
        }

        var limiteCuerpo = http.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (limiteCuerpo is { IsReadOnly: false })
        {
            limiteCuerpo.MaxRequestBodySize = tope;
        }

        // Lo mismo que hace [RequestFormLimits] por dentro: el lector del
        // formulario toma sus limites de este feature
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
