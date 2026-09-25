namespace MenteActiva.Api.Services;

// Implementacion para trabajar sin buzon: en vez de mandar el correo, lo escribe
// en el registro de la aplicacion. Asi se puede seguir todo el flujo de
// invitacion y recuperacion copiando el enlace de la consola.
public class CorreoEnRegistro : IServicioCorreo
{
    private readonly ILogger<CorreoEnRegistro> _registro;

    public CorreoEnRegistro(ILogger<CorreoEnRegistro> registro)
    {
        _registro = registro;
    }

    public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        _registro.LogInformation(
            "Correo no enviado (proveedor Registro). Para: {Destinatario}. Asunto: {Asunto}.\n{Cuerpo}",
            destinatario, asunto, cuerpoHtml);

        return Task.CompletedTask;
    }
}
