namespace MenteActiva.Api.Infraestructura;

// Una regla del negocio que no se cumplio (horario ocupado, cita en el pasado...).
// No es un fallo del sistema: el usuario puede corregirlo, por eso se responde 400
// con el mensaje tal cual.
public sealed class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje)
        : base(mensaje)
    {
    }

    public ReglaNegocioException(string mensaje, Exception interna)
        : base(mensaje, interna)
    {
    }
}