using System.Globalization;

namespace MenteActiva.Services;

// El unico lugar que decide como se escribe un monto. Antes el portal lo hacia con
// ToString("C") y su propia CultureInfo declarada dentro de cada vista, el panel con
// un Intl.NumberFormat distinto en cada archivo de JavaScript, y ocho vistas tenian
// el simbolo escrito a mano: el mismo pago se leia con decimales en el portal y sin
// decimales en el panel.
//
// Dos decisiones viven aqui y no se repiten en ningun otro lado:
//
// - Sin decimales. Son colones y los centimos no se usan; la pantalla de
//   Configuracion ya valida los precios como enteros.
// - El simbolo sale del codigo ISO que guarda TB_CONFIGURACION, no de una
//   constante. Si manana la fila Moneda dice USD, los montos dicen USD.
//
// La gemela en JavaScript es wwwroot/js/formato.js y tiene que producir el mismo
// texto. Se comprobo midiendo las dos: con es-CR y CRC, .NET y el navegador dan los
// mismos caracteres, simbolo pegado al numero y espacio duro (U+00A0) de separador
// de miles. Si se cambia una, hay que cambiar la otra.
public static class FormatoDinero
{
    public const string MonedaPorDefecto = "CRC";

    // La cultura de los montos no es la del sistema operativo a proposito: en el
    // Ubuntu de produccion seria la del servidor y los numeros cambiarian de forma
    private const string CulturaMontos = "es-CR";

    private const string SimboloColon = "₡";

    public static string Escribir(decimal monto, string? codigoMoneda)
        => monto.ToString("C", FormatoDe(codigoMoneda));

    // Para las etiquetas que solo nombran la moneda, del tipo "Monto (...)"
    public static string Simbolo(string? codigoMoneda)
        => FormatoDe(codigoMoneda).CurrencySymbol;

    public static string Normalizar(string? codigoMoneda)
    {
        var codigo = codigoMoneda?.Trim();

        return string.IsNullOrEmpty(codigo) ? MonedaPorDefecto : codigo.ToUpperInvariant();
    }

    private static NumberFormatInfo FormatoDe(string? codigoMoneda)
    {
        var codigo = Normalizar(codigoMoneda);

        var formato = (NumberFormatInfo)CultureInfo
            .GetCultureInfo(CulturaMontos)
            .NumberFormat
            .Clone();

        formato.CurrencyDecimalDigits = 0;

        if (codigo != MonedaPorDefecto)
        {
            // Sin tabla de simbolos por moneda: se escribe el codigo, que es lo
            // mismo que hace Intl.NumberFormat en el navegador ("USD 80 000")
            formato.CurrencySymbol = codigo;
            formato.CurrencyPositivePattern = 2;
            formato.CurrencyNegativePattern = 12;
        }
        else
        {
            formato.CurrencySymbol = SimboloColon;
        }

        return formato;
    }
}
