namespace MenteActiva.Api.Services;

// Los dos correos de la cuenta se arman aqui para que no queden dos plantillas
// parecidas repartidas por el codigo.
//
// El HTML va con estilos en linea y armado con tablas a proposito. Los clientes
// de correo ignoran las hojas de estilo y no entienden flex ni grid, asi que lo
// que aqui se ve raro es lo unico que se ve igual en Gmail, Outlook y el movil.
public static class MensajesCuenta
{
    public const string AsuntoInvitacion = "Active su cuenta de Mente Activa";
    public const string AsuntoRecuperacion = "Restablezca su contraseña de Mente Activa";

    private const string Primario = "#4F6FAE";
    private const string Fondo = "#F7F9FC";
    private const string Superficie = "#FFFFFF";
    private const string Borde = "#E3E8EF";
    private const string Texto = "#2E3A48";
    private const string TextoSuave = "#6B7A8C";

    // Poppins e Inter no existen en los clientes de correo y Gmail bloquea las
    // fuentes externas, asi que se cae a las del sistema.
    private const string Fuente = "'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

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

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{titulo}</title>
            </head>
            <body style="margin:0; padding:0; background-color:{Fondo};">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:{Fondo}; padding:24px 12px;">
              <tr>
                <td align="center">
                  <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="600" style="width:100%; max-width:600px; background-color:{Superficie}; border:1px solid {Borde}; border-radius:12px;">

                    <tr>
                      <td style="background-color:{Primario}; padding:24px 32px; border-radius:12px 12px 0 0;">
                        <p style="margin:0; font-family:{Fuente}; font-size:20px; font-weight:bold; color:#FFFFFF;">Mente Activa</p>
                        <p style="margin:4px 0 0 0; font-family:{Fuente}; font-size:13px; color:#DCE5F4;">Psicopedagogía</p>
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:32px;">
                        <p style="margin:0 0 20px 0; font-family:{Fuente}; font-size:22px; font-weight:bold; color:{Texto};">{titulo}</p>
                        <p style="margin:0 0 16px 0; font-family:{Fuente}; font-size:16px; line-height:24px; color:{Texto};">Hola {saludo},</p>
                        <p style="margin:0 0 28px 0; font-family:{Fuente}; font-size:16px; line-height:24px; color:{Texto};">{parrafo}</p>

                        <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                          <tr>
                            <td style="background-color:{Primario}; border-radius:8px;">
                              <a href="{enlace}" style="display:inline-block; padding:14px 30px; font-family:{Fuente}; font-size:16px; font-weight:bold; color:#FFFFFF; text-decoration:none;">{textoBoton}</a>
                            </td>
                          </tr>
                        </table>

                        <p style="margin:28px 0 8px 0; font-family:{Fuente}; font-size:14px; line-height:22px; color:{TextoSuave};">El enlace vence en {vencimiento} y sirve una sola vez.</p>
                        <p style="margin:0 0 24px 0; font-family:{Fuente}; font-size:14px; line-height:22px; color:{TextoSuave};">Si el botón no funciona, copie esta dirección en su navegador:</p>
                        <p style="margin:0; font-family:{Fuente}; font-size:13px; line-height:20px; color:{Primario}; word-break:break-all;">{enlace}</p>

                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:28px 0 0 0;">
                          <tr><td style="border-top:1px solid {Borde}; font-size:0; line-height:0;">&nbsp;</td></tr>
                        </table>

                        <p style="margin:20px 0 0 0; font-family:{Fuente}; font-size:14px; line-height:22px; color:{TextoSuave};">{cierre}</p>
                      </td>
                    </tr>

                    <tr>
                      <td style="background-color:{Fondo}; border-top:1px solid {Borde}; padding:20px 32px; border-radius:0 0 12px 12px;">
                        <p style="margin:0; font-family:{Fuente}; font-size:12px; line-height:18px; color:{TextoSuave};">Mente Activa Psicopedagogía &middot; Correo automático, por favor no responda a esta dirección.</p>
                      </td>
                    </tr>

                  </table>
                </td>
              </tr>
            </table>
            </body>
            </html>
            """;
    }

    private static string Enlace(string urlBaseWeb, string token)
        => $"{urlBaseWeb.TrimEnd('/')}/Cuenta/DefinirContrasena?token={token}";
}
