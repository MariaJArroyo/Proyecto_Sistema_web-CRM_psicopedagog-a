namespace MenteActiva.Models;

// Aspecto de cada estado de TB_ESTADO_CLIENTE en un solo lugar. Se decide por el Id,
// no por el nombre: si el nombre cambia en la base, el color se mantiene.
// clientes.js usa el mismo criterio (ESTILOS_ESTADO).
public static class EstiloEstadoCliente
{
    private const string ClasePorDefecto = "chip-estado-otro";
    private const string IconoPorDefecto = "bi-circle";

    private static readonly IReadOnlyDictionary<int, (string Clase, string Icono)> Estilos =
        new Dictionary<int, (string Clase, string Icono)>
        {
            [1] = ("chip-estado-nuevo", "bi-stars"),               // Nuevo
            [2] = ("chip-estado-contactado", "bi-telephone"),      // Contactado
            [3] = ("chip-estado-cita", "bi-calendar-check"),       // Cita agendada
            [4] = ("chip-estado-activo", "bi-check-circle"),       // Activo
            [5] = ("chip-estado-inactivo", "bi-pause-circle")      // Inactivo
        };

    public static string Clase(int idEstadoCliente)
        => Estilos.TryGetValue(idEstadoCliente, out var estilo) ? estilo.Clase : ClasePorDefecto;

    public static string Icono(int idEstadoCliente)
        => Estilos.TryGetValue(idEstadoCliente, out var estilo) ? estilo.Icono : IconoPorDefecto;
}