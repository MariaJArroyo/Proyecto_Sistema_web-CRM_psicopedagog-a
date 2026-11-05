using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Infraestructura;

// Lo que decide como corre el worker de avisos. Va en appsettings y no en
// TB_CONFIGURACION a proposito: es infraestructura, no una regla del negocio.
// Las horas de anticipacion del recordatorio SI son del negocio y se quedan en
// la base, donde la pantalla de Configuracion ya las edita.
public sealed class OpcionesNotificaciones
{
    public const string Seccion = "Notificaciones";

    [Range(1, 1440)]
    public int MinutosRevision { get; init; } = 5;

    // Despues de este numero de fallos la notificacion deja de reintentarse y
    // queda marcada como fallida, con el motivo a la vista.
    [Range(1, 20)]
    public int MaxIntentos { get; init; } = 3;

    // Tope por vuelta, para que una cola grande no deje al worker mandando
    // correos media hora seguida
    [Range(1, 200)]
    public int PorVuelta { get; init; } = 50;
}
