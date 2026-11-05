using System.Text;
using System.Text.RegularExpressions;

namespace MenteActiva.Api.Infraestructura;

// La lista cerrada de marcadores y las dos cosas que se hacen con ella:
// validar una plantilla al guardarla y sustituirla al usarla.
//
// Vive aqui y no en el servicio de plantillas porque el envio de correo y los
// avisos de cita tambien la necesitan, y tener la lista en dos lados termina
// con un marcador que una pantalla acepta y la otra no.
//
// La lista es cerrada a proposito: un marcador que no este aqui no se sustituye
// y el texto no sale. Es la diferencia entre un correo con un hueco y un correo
// que le llega a la familia diciendo "Hola [Encargdo]".
public static class MarcadoresPlantilla
{
    public const string Encargado = "Encargado";
    public const string Estudiante = "Estudiante";
    public const string Fecha = "Fecha";
    public const string Hora = "Hora";
    public const string Consultorio = "Consultorio";
    public const string Motivo = "Motivo";

    public static readonly IReadOnlySet<string> Permitidos =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Encargado, Estudiante, Fecha, Hora, Consultorio, Motivo
        };

    // Cualquier cosa entre corchetes que no tenga corchetes adentro. Se busca
    // asi, y no solo los de la lista, para poder avisar del que esta mal escrito
    // en vez de dejarlo pasar como texto.
    private static readonly Regex Patron = new(@"\[([^\[\]]*)\]", RegexOptions.Compiled);

    // Los marcadores que aparecen en el texto, sin repetir y en el orden en que
    // salen. Incluye los que no estan permitidos: de eso se encarga quien llama.
    public static IReadOnlyList<string> Encontrados(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return [];
        }

        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var orden = new List<string>();

        foreach (var coincidencia in Patron.Matches(texto).Cast<Match>())
        {
            var nombre = coincidencia.Groups[1].Value.Trim();

            if (vistos.Add(nombre))
            {
                orden.Add(nombre);
            }
        }

        return orden;
    }

    // Los que no estan en la lista. Lista vacia quiere decir que el texto esta
    // bien escrito, no que no tenga marcadores.
    public static IReadOnlyList<string> NoPermitidos(string? texto)
        => Encontrados(texto).Where(m => !Permitidos.Contains(m)).ToList();

    // Sustituye los que vengan en el diccionario. Lo que no venga se queda tal
    // cual, para que SinResolver lo pueda detectar despues: nunca se reemplaza
    // por vacio a la callada, porque un correo que dice "la cita de  el  a las "
    // es peor que uno que no sale.
    public static string Sustituir(string? texto, IReadOnlyDictionary<string, string?> valores)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        return Patron.Replace(texto, coincidencia =>
        {
            var nombre = coincidencia.Groups[1].Value.Trim();

            if (!valores.TryGetValue(nombre, out var valor) || string.IsNullOrWhiteSpace(valor))
            {
                return coincidencia.Value;
            }

            return valor;
        });
    }

    // Lo que quedo sin sustituir despues de pasar por Sustituir. El envio se
    // niega cuando esto no viene vacio.
    public static IReadOnlyList<string> SinResolver(string? textoYaSustituido)
        => Encontrados(textoYaSustituido);

    // Para el mensaje de error: "[Encargdo], [Fehca]"
    public static string Enumerar(IEnumerable<string> marcadores)
    {
        var texto = new StringBuilder();

        foreach (var marcador in marcadores)
        {
            if (texto.Length > 0)
            {
                texto.Append(", ");
            }

            texto.Append('[').Append(marcador).Append(']');
        }

        return texto.ToString();
    }

    // La lista para mostrarla en la pantalla, ya con los corchetes
    public static IReadOnlyList<string> ParaMostrar()
        => Permitidos.Select(m => $"[{m}]").ToList();
}
