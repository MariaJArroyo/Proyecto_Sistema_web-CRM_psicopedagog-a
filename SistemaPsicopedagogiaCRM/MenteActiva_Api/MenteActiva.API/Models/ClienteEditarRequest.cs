using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

public class ClienteEditarRequest : ClienteRequest
{
    [Range(1, int.MaxValue)]
    public int IdEstadoCliente { get; set; }
}