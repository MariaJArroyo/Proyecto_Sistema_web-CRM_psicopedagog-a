using System.Globalization;
using System.Net;
using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

// Los dos correos que salen cuando alguien llena el formulario del sitio
// publico: el aviso al consultorio y la confirmacion a quien escribio. Comparten
// el marco con los demas correos del sistema (CascaronCorreo) y las dos citan el
// texto del visitante, que es lo mismo que se ve en el panel.
//
// Todo lo que escribio una persona pasa por HtmlEncode antes de entrar al HTML,
// incluidos los datos del consultorio: los escribe la profesional en la pantalla
// de configuracion y tampoco son de fiar a ciegas.
public static class MensajesSolicitudContacto
{
    public const string AsuntoAviso = "Nueva solicitud de contacto - Mente Activa";

    private const string ConsultorioPorDefecto = "Mente Activa";

    // El mismo formato de fecha y hora que ya usan los recordatorios de cita
    private const string FormatoFecha = "dd/MM/yyyy";
    private const string FormatoHora = "HH:mm";

    public static string AsuntoConfirmacion(DatosConsultorioResponse consultorio)
        => $"Recibimos su mensaje - {Nombre(consultorio)}";

    // Aviso interno. Lleva los datos en tabla y el boton al panel, para que la
    // profesional pueda atenderla sin tener que buscarla.
    public static string Aviso(
        int idSolicitud,
        SolicitudContactoRequest peticion,
        string? servicio,
        DateTime fechaRegistro,
        DatosConsultorioResponse consultorio,
        string urlPanel)
    {
        var filas = string.Concat(
            Fila("Nombre", C($"{peticion.Nombre} {peticion.Apellido}".Trim())),
            Fila("Telefono", Enlazar($"tel:{Digitos(peticion.Telefono)}", Telefono(peticion.Telefono))),
            Fila("Correo", Enlazar($"mailto:{peticion.Correo.Trim()}", C(peticion.Correo.Trim()))),
            Fila("Servicio", string.IsNullOrWhiteSpace(servicio) ? "Sin indicar" : C(servicio)),
            Fila("Recibida", C(FechaYHora(fechaRegistro))));

        var contenido = string.Concat(
            CascaronCorreo.Titulo("Nueva solicitud de contacto"),
            CascaronCorreo.Parrafo(
                $"Entro la solicitud #{idSolicitud} por el formulario del sitio publico."),
            Tabla(filas),
            CascaronCorreo.Parrafo("Esto escribio:", "0 0 10px 0"),
            CascaronCorreo.Cita(TextoCitado(peticion.Mensaje)),
            CascaronCorreo.Boton(urlPanel, "Ver la solicitud en el panel"));

        return CascaronCorreo.Armar(
            titulo: "Nueva solicitud de contacto",
            encabezado: C(Nombre(consultorio)),
            subEncabezado: null,
            contenido: contenido,
            pie: $"{C(Nombre(consultorio))} &middot; Aviso automatico del formulario del sitio publico.");
    }

    // Confirmacion a quien escribio. No promete ningun plazo a proposito: el
    // consultorio es unipersonal y no hay nadie que garantice una respuesta en
    // tantas horas.
    public static string Confirmacion(
        SolicitudContactoRequest peticion,
        DateTime fechaRegistro,
        DatosConsultorioResponse consultorio)
    {
        var nombreConsultorio = C(Nombre(consultorio));

        var contenido = new System.Text.StringBuilder();

        contenido.Append(CascaronCorreo.Titulo("Recibimos su mensaje"));
        contenido.Append(CascaronCorreo.Parrafo($"Hola {C(peticion.Nombre.Trim())},"));
        contenido.Append(CascaronCorreo.Parrafo(
            $"Gracias por escribir a {nombreConsultorio}. Su mensaje ya llego y lo vamos a leer con atencion. "
            + "Nos contactaremos con usted pronto."));
        contenido.Append(CascaronCorreo.Parrafo(
            $"Esto nos escribio el {C(FechaYHora(fechaRegistro))}:", "24px 0 10px 0"));
        contenido.Append(CascaronCorreo.Cita(TextoCitado(peticion.Mensaje)));

        var opciones = Opciones(consultorio);

        if (opciones.Count > 0)
        {
            contenido.Append(CascaronCorreo.Separador("8px 0 0 0"));
            contenido.Append(CascaronCorreo.Parrafo(
                $"Si quiere agregar algo o corregir un dato, puede {string.Join(" o ", opciones)}.",
                "20px 0 0 0"));
        }

        if (!string.IsNullOrWhiteSpace(consultorio.DireccionConsultorio))
        {
            contenido.Append(CascaronCorreo.Parrafo(
                $"Nos encontramos en {C(consultorio.DireccionConsultorio.Trim())}.", "8px 0 0 0"));
        }

        return CascaronCorreo.Armar(
            titulo: "Recibimos su mensaje",
            encabezado: nombreConsultorio,
            subEncabezado: null,
            contenido: contenido.ToString(),
            pie: $"{nombreConsultorio} &middot; Este correo salio solo, al recibir su mensaje.");
    }

    // Las formas de contestar que esten configuradas. Si no hay ninguna, el
    // correo no inventa una direccion: simplemente no lleva el parrafo.
    private static List<string> Opciones(DatosConsultorioResponse consultorio)
    {
        var opciones = new List<string>();

        var correo = consultorio.CorreoContacto?.Trim();

        if (!string.IsNullOrWhiteSpace(correo))
        {
            opciones.Add($"escribirnos a {Enlazar($"mailto:{correo}", C(correo))}");
        }

        var telefono = consultorio.TelefonoContacto?.Trim();

        if (!string.IsNullOrWhiteSpace(telefono))
        {
            opciones.Add($"llamarnos al {Enlazar($"tel:{Digitos(telefono)}", Telefono(telefono))}");
        }

        return opciones;
    }

    private static string TextoCitado(string mensaje)
        => string.Join("<br><br>", CascaronCorreo.ParrafosDeTextoPlano(mensaje));

    private static string Tabla(string filas)
        => $"""
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:0 0 28px 0;">
              {filas}
            </table>
            """;

    private static string Fila(string etiqueta, string valorHtml)
        => $"""
            <tr>
              <td style="padding:5px 0; font-family:{CascaronCorreo.Fuente}; font-size:14px; line-height:22px; color:{CascaronCorreo.TextoSuave}; vertical-align:top; white-space:nowrap;">{etiqueta}</td>
              <td style="padding:5px 0 5px 16px; font-family:{CascaronCorreo.Fuente}; font-size:15px; line-height:22px; color:{CascaronCorreo.Texto}; vertical-align:top;">{valorHtml}</td>
            </tr>
            """;

    private static string Enlazar(string destino, string textoHtml)
        => $"""<a href="{C(destino)}" style="color:{CascaronCorreo.Primario}; text-decoration:none;">{textoHtml}</a>""";

    // Los telefonos se guardan con 8 digitos; se parten para que se lean
    private static string Telefono(string? telefono)
    {
        var digitos = Digitos(telefono);

        return C(digitos.Length == 8 ? digitos.Insert(4, " ") : (telefono ?? string.Empty).Trim());
    }

    private static string Digitos(string? texto)
        => new(( texto ?? string.Empty).Where(char.IsDigit).ToArray());

    private static string FechaYHora(DateTime fecha)
        => $"{fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture)} a las "
         + $"{fecha.ToString(FormatoHora, CultureInfo.InvariantCulture)}";

    private static string Nombre(DatosConsultorioResponse consultorio)
        => string.IsNullOrWhiteSpace(consultorio.NombreConsultorio)
            ? ConsultorioPorDefecto
            : consultorio.NombreConsultorio.Trim();

    private static string C(string? texto)
        => WebUtility.HtmlEncode(texto ?? string.Empty);
}
