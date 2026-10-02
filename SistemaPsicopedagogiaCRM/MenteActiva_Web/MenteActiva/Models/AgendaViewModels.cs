using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

public sealed class CatalogosAgendaViewModel
{
    public List<TipoSesionViewModel> TiposSesion { get; set; } = new();
    public List<ModalidadViewModel> Modalidades { get; set; } = new();
}

public sealed class TipoSesionViewModel
{
    public int IdTipoSesion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
}

public sealed class ModalidadViewModel
{
    public int IdModalidad { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class CrearCitaViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante.")]
    public int IdEstudiante { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de sesión.")]
    public int IdTipoSesion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la modalidad.")]
    public int IdModalidad { get; set; }

    [Required(ErrorMessage = "Seleccione un horario.")]
    public DateTime? FechaHoraInicio { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden pasar de 500 caracteres.")]
    public string? Observaciones { get; set; }
}

public sealed class CrearGrupoViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de sesión.")]
    public int IdTipoSesion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la modalidad.")]
    public int IdModalidad { get; set; }

    [Required(ErrorMessage = "Seleccione un horario.")]
    public DateTime? FechaHoraInicio { get; set; }

    [StringLength(100, ErrorMessage = "El nombre del grupo no puede pasar de 100 caracteres.")]
    public string? Nombre { get; set; }

    [Range(2, 30, ErrorMessage = "El cupo del grupo debe estar entre 2 y 30.")]
    public int CupoMaximo { get; set; } = 6;

    [MinLength(1, ErrorMessage = "Agregue al menos un estudiante al grupo.")]
    public List<int> IdsEstudiantes { get; set; } = new();

    [StringLength(500, ErrorMessage = "Las observaciones no pueden pasar de 500 caracteres.")]
    public string? Observaciones { get; set; }
}