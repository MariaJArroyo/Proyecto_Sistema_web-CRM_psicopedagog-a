using System.Security.Cryptography;

namespace MenteActiva.Api.Services;

// El texto del token lo genera la aplicacion, no la base. Va en la URL, asi que
// se cambian los caracteres que ahi significan otra cosa.
public static class TokenEnlace
{
    public static string Generar()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
