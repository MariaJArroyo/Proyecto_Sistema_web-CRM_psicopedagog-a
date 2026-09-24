namespace MenteActiva.Models
{
    public class ServicioViewModel
    {
        public int IdServicio { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal? PrecioReferencia { get; set; }
        public bool Activo { get; set; }
    }
}
