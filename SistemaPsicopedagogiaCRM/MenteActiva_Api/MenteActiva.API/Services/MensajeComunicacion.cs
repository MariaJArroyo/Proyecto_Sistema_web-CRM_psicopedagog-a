using System.Net;
using System.Text;

namespace MenteActiva.Api.Services;

// Los correos que escribe la profesional (M10), aparte de MensajesCuenta, que
// arma los dos de la cuenta. Los dos comparten el marco y la paleta, que viven
// en CascaronCorreo; lo que cambia es el cuerpo: el de la cuenta esta construido
// alrededor de un boton con vencimiento y este es texto y nada mas.
public static class MensajeComunicacion
{
    // El cuerpo llega como texto plano escrito en un textarea. Se escapa primero
    // (nadie pega HTML en un correo nuestro por accidente ni a proposito) y
    // despues se parte en parrafos por linea en blanco, para que los saltos que
    // la persona escribio se vean en el correo.
    public static string Armar(string asunto, string cuerpoTextoPlano, string nombreConsultorio)
    {
        var consultorio = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(nombreConsultorio) ? "Mente Activa" : nombreConsultorio);

        var asuntoSeguro = WebUtility.HtmlEncode(asunto);

        var contenido = new StringBuilder(CascaronCorreo.Titulo(asuntoSeguro));

        foreach (var parrafo in CascaronCorreo.ParrafosDeTextoPlano(cuerpoTextoPlano))
        {
            contenido.Append(CascaronCorreo.Parrafo(parrafo));
        }

        return CascaronCorreo.Armar(
            titulo: asuntoSeguro,
            encabezado: consultorio,
            subEncabezado: null,
            contenido: contenido.ToString(),
            pie: $"{consultorio} &middot; Si necesita responder, escriba al correo de contacto del consultorio.");
    }
}
