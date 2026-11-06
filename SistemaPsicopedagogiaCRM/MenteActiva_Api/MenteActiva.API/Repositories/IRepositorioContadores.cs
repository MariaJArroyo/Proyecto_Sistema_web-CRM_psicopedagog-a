namespace MenteActiva.Api.Repositories;

// Los numeros de las burbujas rojas del menu. Van juntos y no repartidos en el
// repositorio de cada modulo porque no son una funcionalidad de ningun modulo:
// son lo que alimenta un componente de la pantalla, el sidebar.
//
// Los cuatro son una sola cuenta sin reglas de negocio, asi que ninguno puede
// fallar con SIGNAL: si algo se cae, el que responde es el controlador puente
// del Web devolviendo cero, porque una burbuja caida no debe romper el panel.
public interface IRepositorioContadores
{
    // Encargados activos, con correo, que todavia no tienen cuenta de portal
    Task<int> EncargadosSinInvitarAsync(CancellationToken cancelacion);

    // Avisos que agotaron sus intentos y ya no se reintentan
    Task<int> NotificacionesFallidasAsync(int maxIntentos, CancellationToken cancelacion);

    // Citas cuya hora ya paso y que nadie anoto como sesion
    Task<int> CitasSinSesionAsync(DateTime ahora, CancellationToken cancelacion);

    // Cobros en estado "Vencido"
    Task<int> PagosVencidosAsync(CancellationToken cancelacion);
}
