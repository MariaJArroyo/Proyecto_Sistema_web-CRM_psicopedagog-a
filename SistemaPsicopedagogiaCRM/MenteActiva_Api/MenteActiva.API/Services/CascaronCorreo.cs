using System.Net;

namespace MenteActiva.Api.Services;

// El marco HTML que comparten todos los correos que salen del sistema: la banda
// con el nombre del consultorio, el cuerpo y el pie. Antes el marco y la paleta
// estaban escritos dos veces, en constantes identicas.
//
// Va con estilos en linea y armado con tablas a proposito: los clientes de
// correo ignoran las hojas de estilo y no entienden flex ni grid, asi que esto,
// que aqui se ve raro, es lo unico que se ve igual en Gmail, Outlook y el movil.
//
// Importante: aqui no se codifica nada. Todo lo que entra es HTML ya armado, y
// pasar por HtmlEncode lo que escribio una persona le toca a quien llama.
public static class CascaronCorreo
{
    public const string Primario = "#4F6FAE";
    public const string Fondo = "#F7F9FC";
    public const string Superficie = "#FFFFFF";
    public const string Borde = "#E3E8EF";
    public const string Texto = "#2E3A48";
    public const string TextoSuave = "#6B7A8C";

    // Poppins e Inter no existen en los clientes de correo y Gmail bloquea las
    // fuentes externas, asi que se cae a las del sistema.
    public const string Fuente = "'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

    // subEncabezado es la segunda linea chica de la banda. Va nula cuando el
    // encabezado ya trae el nombre completo del consultorio.
    public static string Armar(
        string titulo,
        string encabezado,
        string? subEncabezado,
        string contenido,
        string pie)
    {
        var marca =
            $"""<p style="margin:0; font-family:{Fuente}; font-size:20px; font-weight:bold; color:#FFFFFF;">{encabezado}</p>""";

        if (!string.IsNullOrWhiteSpace(subEncabezado))
        {
            marca +=
                $"""<p style="margin:4px 0 0 0; font-family:{Fuente}; font-size:13px; color:#DCE5F4;">{subEncabezado}</p>""";
        }

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
                        {marca}
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:32px;">
                        {contenido}
                      </td>
                    </tr>

                    <tr>
                      <td style="background-color:{Fondo}; border-top:1px solid {Borde}; padding:20px 32px; border-radius:0 0 12px 12px;">
                        <p style="margin:0; font-family:{Fuente}; font-size:12px; line-height:18px; color:{TextoSuave};">{pie}</p>
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

    // Los pedazos que se repiten dentro del cuerpo. Estan aqui para que los
    // correos usen el mismo tamanio y el mismo color sin copiarse los estilos.

    public static string Titulo(string html)
        => $"""<p style="margin:0 0 20px 0; font-family:{Fuente}; font-size:22px; font-weight:bold; color:{Texto};">{html}</p>""";

    public static string Parrafo(string html, string margen = "0 0 16px 0")
        => $"""<p style="margin:{margen}; font-family:{Fuente}; font-size:16px; line-height:24px; color:{Texto};">{html}</p>""";

    // Lo chico y gris: avisos al pie del cuerpo, no el texto principal
    public static string Nota(string html, string margen = "0 0 16px 0")
        => $"""<p style="margin:{margen}; font-family:{Fuente}; font-size:14px; line-height:22px; color:{TextoSuave};">{html}</p>""";

    public static string Boton(string enlace, string texto)
        => $"""
            <table role="presentation" cellpadding="0" cellspacing="0" border="0">
              <tr>
                <td style="background-color:{Primario}; border-radius:8px;">
                  <a href="{enlace}" style="display:inline-block; padding:14px 30px; font-family:{Fuente}; font-size:16px; font-weight:bold; color:#FFFFFF; text-decoration:none;">{texto}</a>
                </td>
              </tr>
            </table>
            """;

    public static string Separador(string margen = "28px 0 0 0")
        => $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:{margen};">
              <tr><td style="border-top:1px solid {Borde}; font-size:0; line-height:0;">&nbsp;</td></tr>
            </table>
            """;

    // Texto plano escrito en un textarea convertido a HTML. Se escapa primero
    // (nadie manda HTML en un correo nuestro, ni por accidente ni a proposito) y
    // despues se parte en parrafos por linea en blanco, para que los saltos que
    // escribio la persona se vean en el correo.
    public static IEnumerable<string> ParrafosDeTextoPlano(string? texto)
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

    // El recuadro donde se cita, tal como lo escribio, lo que mando una persona
    public static string Cita(string html, string margen = "0 0 24px 0")
        => $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:{margen}; background-color:{Fondo}; border:1px solid {Borde}; border-radius:8px;">
              <tr>
                <td style="padding:16px 20px; border-left:4px solid {Primario};">
                  <p style="margin:0; font-family:{Fuente}; font-size:15px; line-height:23px; color:{Texto};">{html}</p>
                </td>
              </tr>
            </table>
            """;
}
