namespace MenteActiva.Api.Services;

// Los dos correos de la cuenta se arman aqui para que no queden dos plantillas
// parecidas repartidas por el codigo.
public static class MensajesCuenta
{
    public const string AsuntoInvitacion = "Active su cuenta de Mente Activa";
    public const string AsuntoRecuperacion = "Restablezca su contraseña de Mente Activa";

    public static string Invitacion(string urlBaseWeb, string nombre, string token, int horasVigencia)
        => $"""
            <p>Hola {nombre},</p>
            <p>Se le creó una cuenta en el sistema de Mente Activa Psicopedagogía.
            Para entrar, defina su contraseña con el siguiente enlace:</p>
            <p><a href="{Enlace(urlBaseWeb, token)}">Definir mi contraseña</a></p>
            <p>El enlace vence en {horasVigencia} horas y sirve una sola vez.</p>
            <p>Si usted no esperaba este correo, ignórelo.</p>
            """;

    public static string Recuperacion(string urlBaseWeb, string nombre, string token, int horasVigencia)
        => $"""
            <p>Hola {nombre},</p>
            <p>Recibimos una solicitud para restablecer su contraseña.
            Si fue usted, use el siguiente enlace:</p>
            <p><a href="{Enlace(urlBaseWeb, token)}">Restablecer mi contraseña</a></p>
            <p>El enlace vence en {horasVigencia} hora(s) y sirve una sola vez.</p>
            <p>Si usted no lo solicitó, ignore este correo: su contraseña actual sigue funcionando.</p>
            """;

    private static string Enlace(string urlBaseWeb, string token)
        => $"{urlBaseWeb.TrimEnd('/')}/Cuenta/DefinirContrasena?token={token}";
}
