using MenteActiva.Api.Models;

namespace MenteActiva.API.Models
{
    public class ClienteDetalleResponse
    {
        public int Id { get; set; }
        public string NombreEncargado { get; set; } = string.Empty;
        public string ApellidoEncargado { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Observaciones { get; set; }
        public int? IdServicioInteres { get; set; }
        public string? Servicio { get; set; }
        public int IdEstadoCliente { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }

        public List<EstudianteClienteResponse> Estudiantes { get; set; } = new();
    }
}