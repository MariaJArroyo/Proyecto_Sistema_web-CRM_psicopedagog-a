using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

public sealed class ServicioLogError : IServicioLogError
{
    // Los topes son los de las columnas de TB_LOG_ERROR en 01_esquema.sql. Con
    // sql_mode estricto un texto mas largo no se recorta solo: revienta la
    // insercion, y entonces el fallo que se queria registrar se pierde.
    private const int LargoOrigen = 200;
    private const int LargoTipoExcepcion = 200;
    private const int LargoUrl = 500;
    private const int LargoMetodo = 10;
    private const int LargoIp = 45;
    private const int LargoAgente = 500;

    // Mensaje es TEXT y StackTrace LONGTEXT; el tope aqui no es la columna sino
    // no guardar un texto absurdo si una excepcion trae media base adentro
    private const int LargoMensaje = 4000;
    private const int LargoStackTrace = 30000;

    private readonly IRepositorioLogError _repositorio;
    private readonly ILogger<ServicioLogError> _registro;

    public ServicioLogError(IRepositorioLogError repositorio, ILogger<ServicioLogError> registro)
    {
        _repositorio = repositorio;
        _registro = registro;
    }

    public async Task RegistrarAsync(ErrorAplicacion error, CancellationToken cancelacion)
    {
        try
        {
            await _repositorio.GuardarAsync(Recortar(error), cancelacion);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex, "No se pudo guardar el error en TB_LOG_ERROR.");
        }
    }

    private static ErrorAplicacion Recortar(ErrorAplicacion error) => new()
    {
        Nivel = error.Nivel,
        Origen = Cortar(error.Origen, LargoOrigen),
        // Mensaje es NOT NULL: si la excepcion vino sin texto, al menos su tipo
        Mensaje = Cortar(error.Mensaje, LargoMensaje)
            ?? Cortar(error.TipoExcepcion, LargoTipoExcepcion)
            ?? "Error sin mensaje.",
        TipoExcepcion = Cortar(error.TipoExcepcion, LargoTipoExcepcion),
        StackTrace = Cortar(error.StackTrace, LargoStackTrace),
        UrlSolicitada = Cortar(error.UrlSolicitada, LargoUrl),
        MetodoHttp = Cortar(error.MetodoHttp, LargoMetodo),
        IdUsuario = error.IdUsuario,
        DireccionIp = Cortar(error.DireccionIp, LargoIp),
        NavegadorAgente = Cortar(error.NavegadorAgente, LargoAgente)
    };

    private static string? Cortar(string? valor, int largo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return valor.Length <= largo ? valor : valor[..largo];
    }
}
