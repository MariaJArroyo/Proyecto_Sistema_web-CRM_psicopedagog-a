namespace MenteActiva.Api.Models;

// Una fila de TB_LOG_ERROR. Los nombres son los de las columnas porque Dapper
// los calza por nombre contra los parametros de la insercion.
//
// No tiene llaves foraneas a proposito (ver el glosario): IdUsuario guarda
// quien venia en el token, no una referencia que haya que validar. Si ese
// usuario se borra despues, el registro del fallo igual se queda.
public sealed class ErrorAplicacion
{
    // 'Error', 'Advertencia' o el 'Critico' con acento del esquema: lo exige
    // CK_LogError_Nivel
    public string Nivel { get; set; } = "Error";

    // Donde paso: el nombre del endpoint cuando se conoce
    public string? Origen { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public string? TipoExcepcion { get; set; }

    public string? StackTrace { get; set; }

    public string? UrlSolicitada { get; set; }

    public string? MetodoHttp { get; set; }

    public int? IdUsuario { get; set; }

    public string? DireccionIp { get; set; }

    public string? NavegadorAgente { get; set; }
}
