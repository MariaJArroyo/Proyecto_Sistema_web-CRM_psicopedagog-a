using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MenteActiva.Api.Services;

namespace MenteActiva.Api.Infraestructura;

// Lo mismo que LimiteSubidaPdf, para los archivos que suben las familias:
// corta antes de leer el formulario si la peticion ya declara que es mas
// grande que el maximo, y le pone ese tope al lector del multipart. El tamanio
// exacto del archivo lo revisa cada servicio.
public abstract class LimiteSubidaArchivo : IResourceFilter
{
    private const long MargenFormulario = 64 * 1024;

    private readonly long _maximoBytes;
    private readonly string _mensaje;

    protected LimiteSubidaArchivo(long maximoBytes, string mensaje)
    {
        _maximoBytes = maximoBytes;
        _mensaje = mensaje;
    }

    public void OnResourceExecuting(ResourceExecutingContext contexto)
    {
        var http = contexto.HttpContext;
        var tope = _maximoBytes + MargenFormulario;

        if (http.Request.ContentLength > tope)
        {
            contexto.Result = new BadRequestObjectResult(new { mensaje = _mensaje });
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

// Comprobante de pago (ServicioReportesPago)
public sealed class LimiteSubidaComprobante : LimiteSubidaArchivo
{
    public LimiteSubidaComprobante()
        : base(AlmacenComprobantes.TamanoMaximoBytes,
               $"El comprobante no puede pasar de {AlmacenComprobantes.TamanoMaximoMb} MB.")
    {
    }
}

// Foto del estudiante en la ficha (FotosEstudianteController)
public sealed class LimiteSubidaFotoEstudiante : LimiteSubidaArchivo
{
    public LimiteSubidaFotoEstudiante()
        : base(AlmacenFotosEstudiante.TamanoMaximoBytes,
               $"La foto no puede pasar de {AlmacenFotosEstudiante.TamanoMaximoMb} MB.")
    {
    }
}

// Archivo de una tarea entregada (ServicioEntregasTarea)
public sealed class LimiteSubidaEntregaTarea : LimiteSubidaArchivo
{
    public LimiteSubidaEntregaTarea()
        : base(AlmacenEntregasTarea.TamanoMaximoBytes,
               $"El archivo no puede pasar de {AlmacenEntregasTarea.TamanoMaximoMb} MB.")
    {
    }
}
