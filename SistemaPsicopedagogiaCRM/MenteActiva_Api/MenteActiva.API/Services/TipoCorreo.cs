namespace MenteActiva.Api.Services;

// De que se trata un correo. Se usa para el distintivo que va arriba del
// cuerpo (un icono y una etiqueta de color), para que la familia vea de un
// vistazo si es un recordatorio, una cancelacion, una alerta, etc.
public enum TipoCorreo
{
    Mensaje,
    Informacion,
    Bienvenida,
    Seguridad,
    Recordatorio,
    Confirmacion,
    Actualizacion,
    Cancelacion,
    Alerta,
    Aprobacion,
    Rechazo,
    Solicitud
}

public static class TiposCorreo
{
    // Emoji: es lo unico que se ve igual en Gmail, Outlook y el movil sin
    // adjuntar imagenes (las fuentes de iconos y el SVG no funcionan en correo).
    // Va como entidad HTML para no depender de la codificacion del mensaje.
    public sealed record Estilo(string Emoji, string Etiqueta, string Fondo, string Color);

    private static readonly Estilo Azul = new("", "", "#E8EEF8", "#3A5488");

    public static Estilo De(TipoCorreo tipo) => tipo switch
    {
        TipoCorreo.Informacion => Azul with { Emoji = "&#x2139;&#xFE0F;", Etiqueta = "Información" },
        TipoCorreo.Bienvenida => new("&#x1F44B;", "Bienvenida", "#E6F4EE", "#2C7A52"),
        TipoCorreo.Seguridad => new("&#x1F510;", "Seguridad de su cuenta", "#FDF1E1", "#8A5A12"),
        TipoCorreo.Recordatorio => Azul with { Emoji = "&#x23F0;", Etiqueta = "Recordatorio" },
        TipoCorreo.Confirmacion => new("&#x2705;", "Confirmado", "#E6F4EE", "#2C7A52"),
        TipoCorreo.Actualizacion => new("&#x1F504;", "Hubo un cambio", "#F1ECFB", "#5B3FA0"),
        TipoCorreo.Cancelacion => new("&#x274C;", "Cancelado", "#FBE7E6", "#B4433E"),
        TipoCorreo.Alerta => new("&#x26A0;&#xFE0F;", "Atención", "#FDF1E1", "#8A5A12"),
        TipoCorreo.Aprobacion => new("&#x1F44D;", "Aprobado", "#E6F4EE", "#2C7A52"),
        TipoCorreo.Rechazo => new("&#x26D4;", "No aprobado", "#FBE7E6", "#B4433E"),
        TipoCorreo.Solicitud => Azul with { Emoji = "&#x1F4E5;", Etiqueta = "Nueva solicitud" },
        _ => Azul with { Emoji = "&#x2709;&#xFE0F;", Etiqueta = "Mensaje del consultorio" }
    };

    // Avisos automaticos: el tipo sale de la clave de la plantilla
    public static TipoCorreo DesdeClavePlantilla(string? clave) => clave?.Trim() switch
    {
        "RecordatorioCita" => TipoCorreo.Recordatorio,
        "ConfirmacionCita" => TipoCorreo.Confirmacion,
        "ActualizacionCita" => TipoCorreo.Actualizacion,
        "CancelacionCita" => TipoCorreo.Cancelacion,
        null or "" => TipoCorreo.Mensaje,
        _ => TipoCorreo.Informacion
    };

    // El que eligio la profesional al escribir el correo. Lo que no se
    // reconoce queda como mensaje comun en vez de dar error.
    public static TipoCorreo Leer(string? texto)
        => Enum.TryParse<TipoCorreo>(texto?.Trim(), ignoreCase: true, out var tipo)
           && Enum.IsDefined(tipo)
            ? tipo
            : TipoCorreo.Mensaje;
}
