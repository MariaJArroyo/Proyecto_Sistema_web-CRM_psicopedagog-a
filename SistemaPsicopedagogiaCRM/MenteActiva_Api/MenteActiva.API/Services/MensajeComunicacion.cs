using System.Net;
using System.Text;

namespace MenteActiva.Api.Services;

// El caparazon HTML de los correos que escribe la profesional (M10), aparte de
// MensajesCuenta, que arma los dos correos de la cuenta.
//
// Los dos comparten la paleta y la tecnica (tablas y estilos en linea, porque
// los clientes de correo ignoran las hojas de estilo y no entienden flex ni
// grid), pero no el molde: el de MensajesCuenta esta construido alrededor de un
// boton, un enlace y un vencimiento, y aqui no hay ninguna de las tres cosas.
// Reutilizarlo obligaba a pasarle textos vacios y a cambiarle la firma, que es
// tocar el correo de invitacion, que ya funciona.
public static class MensajeComunicacion
{
    private const string Primario = "#4F6FAE";
    private const string Fondo = "#F7F9FC";
    private const string Superficie = "#FFFFFF";
    private const string Borde = "#E3E8EF";
    private const string Texto = "#2E3A48";
    private const string TextoSuave = "#6B7A8C";

    private const string Fuente = "'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

    // El cuerpo llega como texto plano escrito en un textarea. Se escapa primero
    // (nadie pega HTML en un correo nuestro por accidente ni a proposito) y
    // despues se parte en parrafos por linea en blanco, para que los saltos que
    // la persona escribio se vean en el correo.
    public static string Armar(string asunto, string cuerpoTextoPlano, string nombreConsultorio)
    {
        var html = new StringBuilder();

        foreach (var parrafo in Parrafos(cuerpoTextoPlano))
        {
            html.Append(
                $"""<p style="margin:0 0 16px 0; font-family:{Fuente}; font-size:16px; line-height:24px; color:{Texto};">{parrafo}</p>""");
        }

        var consultorio = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(nombreConsultorio) ? "Mente Activa" : nombreConsultorio);

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{WebUtility.HtmlEncode(asunto)}</title>
            </head>
            <body style="margin:0; padding:0; background-color:{Fondo};">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:{Fondo}; padding:24px 12px;">
              <tr>
                <td align="center">
                  <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="600" style="width:100%; max-width:600px; background-color:{Superficie}; border:1px solid {Borde}; border-radius:12px;">

                    <tr>
                      <td style="background-color:{Primario}; padding:24px 32px; border-radius:12px 12px 0 0;">
                        <p style="margin:0; font-family:{Fuente}; font-size:20px; font-weight:bold; color:#FFFFFF;">{consultorio}</p>
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:32px;">
                        <p style="margin:0 0 20px 0; font-family:{Fuente}; font-size:22px; font-weight:bold; color:{Texto};">{WebUtility.HtmlEncode(asunto)}</p>
                        {html}
                      </td>
                    </tr>

                    <tr>
                      <td style="background-color:{Fondo}; border-top:1px solid {Borde}; padding:20px 32px; border-radius:0 0 12px 12px;">
                        <p style="margin:0; font-family:{Fuente}; font-size:12px; line-height:18px; color:{TextoSuave};">{consultorio} &middot; Si necesita responder, escriba al correo de contacto del consultorio.</p>
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

    private static IEnumerable<string> Parrafos(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            yield break;
        }

        var normalizado = texto.Replace("\r\n", "\n").Replace('\r', '\n');

        foreach (var bloque in normalizado.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var limpio = bloque.Trim();

            if (limpio.Length == 0)
            {
                continue;
            }

            // Dentro de un parrafo, un salto simple sigue siendo un salto
            yield return WebUtility.HtmlEncode(limpio).Replace("\n", "<br>");
        }
    }
}
