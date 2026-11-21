using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MenteActiva.Api.Services;

// Envio real por SMTP de Gmail. Necesita una cuenta con verificacion en dos
// pasos y una contrasena de aplicacion; la contrasena normal de la cuenta no
// sirve para esto.
public class CorreoGmail : IServicioCorreo
{
    // El logo se copia junto al ejecutable (ver el .csproj)
    private static readonly string _rutaLogo =
        Path.Combine(AppContext.BaseDirectory, "Recursos", "LogoMenteActiva.jpeg");

    private readonly string _servidor;
    private readonly int _puerto;
    private readonly string _remitente;
    private readonly string _nombreRemitente;
    private readonly string _usuario;
    private readonly string _contrasena;

    public CorreoGmail(IConfiguration config)
    {
        _servidor = config["Correo:Servidor"] ?? "smtp.gmail.com";
        _puerto = config.GetValue<int?>("Correo:Puerto") ?? 587;
        _remitente = config["Correo:Remitente"]
            ?? throw new InvalidOperationException("Falta Correo:Remitente en la configuracion.");
        _nombreRemitente = config["Correo:NombreRemitente"] ?? "Mente Activa Psicopedagogia";
        _usuario = config["Correo:Usuario"] ?? _remitente;
        _contrasena = config["Correo:Contrasena"]
            ?? throw new InvalidOperationException("Falta Correo:Contrasena en la configuracion.");
    }

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_nombreRemitente, _remitente));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;

        var constructor = new BodyBuilder { HtmlBody = cuerpoHtml };

        // El logo viaja dentro del mensaje (cid), asi no depende de ninguna URL publica
        if (cuerpoHtml.Contains($"cid:{CascaronCorreo.IdLogo}", StringComparison.Ordinal)
            && File.Exists(_rutaLogo))
        {
            var logo = constructor.LinkedResources.Add(_rutaLogo);
            logo.ContentId = CascaronCorreo.IdLogo;
        }

        mensaje.Body = constructor.ToMessageBody();

        using var cliente = new SmtpClient();

        await cliente.ConnectAsync(_servidor, _puerto, SecureSocketOptions.StartTls);
        await cliente.AuthenticateAsync(_usuario, _contrasena);
        await cliente.SendAsync(mensaje);
        await cliente.DisconnectAsync(true);
    }
}