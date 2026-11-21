namespace MenteActiva.Models;

public class PortalPerfilEstudiantesViewModel
{
    public int IdEstudiante { get; set; }
    public string Estudiante { get; set; } = string.Empty;
    public string? NivelEducativo { get; set; }
    public string? Institucion { get; set; }
    public string Parentesco { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public bool EsPrincipal { get; set; }

    public int? Edad
    {
        get
        {
            if (FechaNacimiento is null) return null;
            var hoy = DateTime.Today;
            var edad = hoy.Year - FechaNacimiento.Value.Year;
            if (FechaNacimiento.Value.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }
    }
}
