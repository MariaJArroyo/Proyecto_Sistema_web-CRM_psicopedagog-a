using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MenteActiva.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace MenteActiva.Api.Services;

// Arma el token que el API entrega al iniciar sesion. La Web lo guarda y lo
// manda de vuelta en cada llamada. La validacion del token se configura aparte.
public class GeneradorTokenAcceso : IGeneradorTokenAcceso
{
    public const string ClaimIdEncargado = "IdEncargado";

    private readonly string _llave;
    private readonly string _emisor;
    private readonly string _audiencia;
    private readonly int _horasVigencia;

    public GeneradorTokenAcceso(IConfiguration config)
    {
        _llave = config["Jwt:Llave"]
            ?? throw new InvalidOperationException("Falta Jwt:Llave en la configuracion.");
        _emisor = config["Jwt:Emisor"] ?? "MenteActiva.Api";
        _audiencia = config["Jwt:Audiencia"] ?? "MenteActiva.Web";
        _horasVigencia = config.GetValue<int?>("Jwt:HorasVigencia") ?? 8;
    }

    public TokenAcceso Generar(UsuarioAutenticado usuario)
    {
        var expira = DateTime.UtcNow.AddHours(_horasVigencia);

        var datos = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Email, usuario.Correo)
        };

        if (!string.IsNullOrWhiteSpace(usuario.Rol))
        {
            datos.Add(new Claim(ClaimTypes.Role, usuario.Rol));
        }

        if (usuario.IdEncargado.HasValue)
        {
            datos.Add(new Claim(ClaimIdEncargado, usuario.IdEncargado.Value.ToString()));
        }

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_llave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _emisor,
            audience: _audiencia,
            claims: datos,
            expires: expira,
            signingCredentials: credenciales);

        return new TokenAcceso(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
