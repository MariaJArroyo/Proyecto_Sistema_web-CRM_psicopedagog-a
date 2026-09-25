namespace MenteActiva.Api.Services;

public interface IServicioCorreo
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml);
}
