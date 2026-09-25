using MenteActiva.Api.Models;

namespace MenteActiva.Api.Services;

public interface IGeneradorTokenAcceso
{
    TokenAcceso Generar(UsuarioAutenticado usuario);
}

public record TokenAcceso(string Token, DateTime Expira);
