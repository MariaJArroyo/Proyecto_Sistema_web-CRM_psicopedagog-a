namespace MenteActiva.Api.Models;

// Resultados posibles de un intento de inicio de sesion. Son los mismos textos
// que acepta el CHECK de TB_ACCESO_USUARIO.
public static class ResultadosAcceso
{
    public const string Exitoso = "Exitoso";
    public const string ContrasenaIncorrecta = "ContrasenaIncorrecta";
    public const string CorreoNoRegistrado = "CorreoNoRegistrado";
    public const string CuentaBloqueada = "CuentaBloqueada";
    public const string CuentaInactiva = "CuentaInactiva";
    public const string PendienteActivacion = "PendienteActivacion";
    public const string SinRol = "SinRol";

    public static readonly IReadOnlyList<string> Todos = new[]
    {
        Exitoso, ContrasenaIncorrecta, CorreoNoRegistrado, CuentaBloqueada,
        CuentaInactiva, PendienteActivacion, SinRol
    };
}

// Un intento en el historial de una cuenta (SP_Acceso_ListarPorUsuario)
public sealed class AccesoUsuarioResponse
{
    public long IdAcceso { get; set; }

    public DateTime FechaHora { get; set; }

    public string Resultado { get; set; } = string.Empty;

    public string? Origen { get; set; }

    public string? Ip { get; set; }

    public string? Navegador { get; set; }
}

// Un renglon de la bitacora completa (SP_Acceso_Listar)
public sealed class AccesoBitacoraResponse
{
    public long IdAcceso { get; set; }

    public DateTime FechaHora { get; set; }

    public int? IdUsuario { get; set; }

    public string? Usuario { get; set; }

    public string Correo { get; set; } = string.Empty;

    public string? Rol { get; set; }

    // Interno, Externo o Desconocido (el correo no existe)
    public string Tipo { get; set; } = string.Empty;

    public string? Origen { get; set; }

    public string Resultado { get; set; } = string.Empty;

    public string? Ip { get; set; }

    public string? Navegador { get; set; }
}

// Filtros de la bitacora completa
public sealed class FiltroAccesosRequest
{
    // Internos, Externos, Desconocidos o vacio
    public string? Tipo { get; set; }

    // Exitoso, Fallido o uno de ResultadosAcceso
    public string? Resultado { get; set; }

    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }

    public string? Texto { get; set; }
}
