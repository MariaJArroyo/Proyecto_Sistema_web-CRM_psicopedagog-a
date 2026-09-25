using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public class ClienteEditarViewModel : ClienteRequestViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un estado.")]
    [Display(Name = "Estado")]
    public int IdEstadoCliente { get; set; }
}