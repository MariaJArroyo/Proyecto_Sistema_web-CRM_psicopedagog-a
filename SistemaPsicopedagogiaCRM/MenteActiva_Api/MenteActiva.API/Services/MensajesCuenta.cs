namespace MenteActiva.Api.Services;

// Los dos correos de la cuenta se arman aqui para que no queden dos plantillas
// parecidas repartidas por el codigo. El marco y la paleta salen de
// CascaronCorreo, que es el mismo de todos los correos del sistema.
public static class MensajesCuenta
{
    public const string AsuntoInvitacion = "Active su cuenta de Mente Activa";
    public const string AsuntoRecuperacion = "Restablezca su contraseña de Mente Activa";

    public static string Invitacion(string urlBaseWeb, string nombre, string token, int horasVigencia)
        => Plantilla(
            titulo: "Le damos la bienvenida",
            saludo: nombre,
            parrafo: "Se le creó una cuenta en el sistema de Mente Activa Psicopedagogía. "
                   + "Para entrar por primera vez, defina su contraseña con el siguiente botón.",
            textoBoton: "Definir mi contraseña",
            enlace: Enlace(urlBaseWeb, token),
            horasVigencia: horasVigencia,
            cierre: "Si usted no esperaba este correo, puede ignorarlo.");

    public static string Recuperacion(string urlBaseWeb, string nombre, string token, int horasVigencia)
        => Plantilla(
            titulo: "Restablecer su contraseña",
            saludo: nombre,
            parrafo: "Recibimos una solicitud para restablecer la contraseña de su cuenta. "
                   + "Si fue usted, continúe con el siguiente botón.",
            textoBoton: "Restablecer mi contraseña",
            enlace: Enlace(urlBaseWeb, token),
            horasVigencia: horasVigencia,
            cierre: "Si usted no lo solicitó, ignore este correo: su contraseña actual sigue funcionando.");

    private static string Plantilla(
        string titulo,
        string saludo,
        string parrafo,
        string textoBoton,
        string enlace,
        int horasVigencia,
        string cierre)
    {
        var vencimiento = horasVigencia == 1 ? "1 hora" : $"{horasVigencia} horas";

        var contenido = string.Concat(
            CascaronCorreo.Titulo(titulo),
            CascaronCorreo.Parrafo($"Hola {saludo},"),
            CascaronCorreo.Parrafo(parrafo, "0 0 28px 0"),
            CascaronCorreo.Boton(enlace, textoBoton),
            CascaronCorreo.Nota($"El enlace vence en {vencimiento} y sirve una sola vez.", "28px 0 8px 0"),
            CascaronCorreo.Nota("Si el botón no funciona, copie esta dirección en su navegador:", "0 0 24px 0"),
            $"""<p style="margin:0; font-family:{CascaronCorreo.Fuente}; font-size:13px; line-height:20px; color:{CascaronCorreo.Primario}; word-break:break-all;">{enlace}</p>""",
            CascaronCorreo.Separador(),
            CascaronCorreo.Nota(cierre, "20px 0 0 0"));

        return CascaronCorreo.Armar(
            titulo: titulo,
            encabezado: "Mente Activa",
            subEncabezado: "Psicopedagogía",
            contenido: contenido,
            pie: "Mente Activa Psicopedagogía &middot; Correo automático, por favor no responda a esta dirección.");
    }

    private static string Enlace(string urlBaseWeb, string token)
        => $"{urlBaseWeb.TrimEnd('/')}/Cuenta/DefinirContrasena?token={token}";
}
