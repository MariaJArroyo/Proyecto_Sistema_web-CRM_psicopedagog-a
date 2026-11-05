using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Casi todo lo decide el procedimiento, que es el unico lugar por donde se
// escribe y el que valida que quien pregunta participe del hilo. Aqui vive lo
// que es forma de la peticion: que un hilo nuevo venga con destinatario, y que
// una respuesta no traiga uno.
public sealed class ServicioMensajes : IServicioMensajes
{
    private readonly IRepositorioMensaje _repositorio;

    public ServicioMensajes(IRepositorioMensaje repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<IReadOnlyList<HiloResponse>> ListarHilosAsync(
        int idUsuario, CancellationToken cancelacion)
        => _repositorio.ListarHilosAsync(idUsuario, cancelacion);

    public Task<IReadOnlyList<MensajeResponse>> AbrirHiloAsync(
        int idUsuario, int idHilo, CancellationToken cancelacion)
    {
        if (idHilo <= 0)
        {
            throw new ReglaNegocioException("La conversación indicada no es válida.");
        }

        return _repositorio.AbrirHiloAsync(idUsuario, idHilo, cancelacion);
    }

    public Task<IReadOnlyList<DestinatarioMensajeResponse>> ListarDestinatariosAsync(
        int idUsuario, CancellationToken cancelacion)
        => _repositorio.ListarDestinatariosAsync(idUsuario, cancelacion);

    public Task<int> ContarSinLeerAsync(int idUsuario, CancellationToken cancelacion)
        => _repositorio.ContarSinLeerAsync(idUsuario, cancelacion);

    public async Task<MensajeEnviadoResponse> EnviarAsync(
        int idUsuario, EnviarMensajeRequest peticion, CancellationToken cancelacion)
    {
        var esNuevo = peticion.IdHilo is null or <= 0;

        if (esNuevo)
        {
            if (peticion.IdUsuarioDestinatario is null or <= 0)
            {
                throw new ReglaNegocioException("Seleccione a quién le quiere escribir.");
            }

            if (string.IsNullOrWhiteSpace(peticion.Asunto))
            {
                throw new ReglaNegocioException("Indique el asunto del mensaje.");
            }
        }

        var (idMensaje, idHilo) = await _repositorio.EnviarAsync(
            idUsuario,
            esNuevo ? peticion.IdUsuarioDestinatario : null,
            esNuevo ? null : peticion.IdHilo,
            esNuevo ? peticion.Asunto : null,
            peticion.Cuerpo,
            cancelacion);

        return new MensajeEnviadoResponse
        {
            IdMensaje = idMensaje,
            IdHilo = idHilo,
            Mensaje = esNuevo ? "Conversación iniciada." : "Mensaje enviado."
        };
    }
}
