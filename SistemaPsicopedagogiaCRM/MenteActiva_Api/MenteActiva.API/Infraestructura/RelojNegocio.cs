using Microsoft.Extensions.Options;

namespace MenteActiva.Api.Infraestructura;

// "Ahora" segun el consultorio, no segun el servidor. Todas las reglas de
// horario usan esto, asi no dependen de la zona horaria de la maquina ni de MySQL.
public interface IRelojNegocio
{
    DateTime Ahora { get; }

    DateOnly Hoy { get; }
}

public sealed class RelojNegocio : IRelojNegocio
{
    private readonly TimeProvider _tiempo;
    private readonly TimeZoneInfo _zona;

    // TimeProvider se inyecta para poder fijar la hora en pruebas
    public RelojNegocio(TimeProvider tiempo, IOptions<OpcionesAgenda> opciones)
    {
        _tiempo = tiempo;
        _zona = ObtenerZona(opciones.Value.ZonaHoraria);
    }

    public DateTime Ahora
        => TimeZoneInfo.ConvertTime(_tiempo.GetUtcNow(), _zona).DateTime;

    public DateOnly Hoy
        => DateOnly.FromDateTime(Ahora);

    // En Windows sin ICU el id IANA puede no existir: se convierte al de Windows
    private static TimeZoneInfo ObtenerZona(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var idWindows))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(idWindows);
            }

            throw new InvalidOperationException($"La zona horaria '{id}' no existe en este equipo.");
        }
    }
}