using System.Text.Json;
using MenteActiva.Api.Infraestructura;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;

namespace MenteActiva.Api.Services;

// Casi todo lo decide el procedimiento, que es el unico lugar por donde se
// escribe y el que valida quien puede ver cada hilo. Aqui vive lo que es forma
// de la peticion: que el hilo nuevo del personal venga con destinatario, que el
// del encargado no lleve (va al consultorio), y que una respuesta no traiga uno.
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
        int idUsuario, bool esEncargado, EnviarMensajeRequest peticion, CancellationToken cancelacion)
    {
        var esNuevo = peticion.IdHilo is null or <= 0;

        if (esNuevo)
        {
            if (!esEncargado && peticion.IdUsuarioDestinatario is null or <= 0)
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
            esNuevo && !esEncargado ? peticion.IdUsuarioDestinatario : null,
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

    public Task<bool> TieneAccesoAsync(int idUsuario, CancellationToken cancelacion)
        => _repositorio.TieneAccesoAsync(idUsuario, cancelacion);

    public Task<IReadOnlyList<AccesoMensajeriaRolResponse>> ListarAccesoAsync(CancellationToken cancelacion)
        => _repositorio.ListarAccesoAsync(cancelacion);

    public Task GuardarAccesoAsync(
        int idUsuarioAccion, GuardarAccesoMensajeriaRequest peticion, CancellationToken cancelacion)
    {
        if (peticion.Roles is null || peticion.Roles.Count == 0)
        {
            throw new ReglaNegocioException("Marque al menos un rol con acceso a los mensajes.");
        }

        // Los ids los valida el procedimiento (solo 1, 2 y 3)
        var rolesJson = JsonSerializer.Serialize(peticion.Roles.Distinct().ToList());

        return _repositorio.GuardarAccesoAsync(idUsuarioAccion, rolesJson, cancelacion);
    }
}
