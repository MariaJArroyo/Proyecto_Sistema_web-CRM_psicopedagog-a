using System.Net;
using System.Net.Sockets;

namespace MenteActiva.Api.Infraestructura;

// Direcciones a las que el verificador de enlaces no se conecta nunca (SSRF):
// bucle local, redes privadas, enlace local y rangos reservados. Sin esto,
// alguien registra http://127.0.0.1:3306 o http://169.254.169.254 como enlace
// y el API le hace la peticion desde adentro de la red del servidor.
//
// Lo que trae una IPv4 metida dentro de una IPv6 (::ffff:10.0.0.1, NAT64,
// 6to4) se revisa por la IPv4 de adentro.
public static class DireccionesBloqueadas
{
    public static bool EsBloqueada(IPAddress direccion)
    {
        if (direccion.IsIPv4MappedToIPv6)
        {
            direccion = direccion.MapToIPv4();
        }

        return direccion.AddressFamily switch
        {
            AddressFamily.InterNetwork => EsIPv4Bloqueada(direccion.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => EsIPv6Bloqueada(direccion),
            _ => true
        };
    }

    private static bool EsIPv4Bloqueada(byte[] b) =>
        b[0] == 0                                        // 0.0.0.0/8
        || b[0] == 10                                    // 10.0.0.0/8
        || b[0] == 127                                   // bucle local
        || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)    // 100.64.0.0/10, CGNAT
        || (b[0] == 169 && b[1] == 254)                  // enlace local, metadatos de nube
        || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)     // 172.16.0.0/12
        || (b[0] == 192 && b[1] == 0 && b[2] == 0)       // 192.0.0.0/24
        || (b[0] == 192 && b[1] == 0 && b[2] == 2)       // documentacion
        || (b[0] == 192 && b[1] == 168)                  // 192.168.0.0/16
        || (b[0] == 198 && (b[1] == 18 || b[1] == 19))   // pruebas de red
        || (b[0] == 198 && b[1] == 51 && b[2] == 100)    // documentacion
        || (b[0] == 203 && b[1] == 0 && b[2] == 113)     // documentacion
        || b[0] >= 224;                                  // multicast, reservado y difusion

    private static bool EsIPv6Bloqueada(IPAddress direccion)
    {
        if (IPAddress.IsLoopback(direccion)
            || direccion.Equals(IPAddress.IPv6Any)
            || direccion.IsIPv6LinkLocal
            || direccion.IsIPv6SiteLocal
            || direccion.IsIPv6UniqueLocal
            || direccion.IsIPv6Multicast
            || direccion.IsIPv6Teredo)
        {
            return true;
        }

        var b = direccion.GetAddressBytes();

        // ::a.b.c.d (compatible con IPv4, en desuso)
        if (b.Take(12).All(x => x == 0))
        {
            return EsIPv4Bloqueada(b[12..16]);
        }

        // 64:ff9b::/96, NAT64
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b
            && b.Skip(4).Take(8).All(x => x == 0))
        {
            return EsIPv4Bloqueada(b[12..16]);
        }

        // 2002::/16, 6to4: la IPv4 va en los bytes 2 a 5
        if (b[0] == 0x20 && b[1] == 0x02)
        {
            return EsIPv4Bloqueada(b[2..6]);
        }

        return false;
    }
}

// La lanza la conexion del verificador cuando el nombre resuelve a una
// direccion bloqueada. Llega envuelta en una HttpRequestException.
public sealed class DireccionBloqueadaException : Exception
{
    public DireccionBloqueadaException(string servidor)
        : base($"El servidor {servidor} resuelve a una direccion no permitida.")
    {
    }
}
