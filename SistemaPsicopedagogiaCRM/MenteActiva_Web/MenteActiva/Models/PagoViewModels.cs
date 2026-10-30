using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Models;

// "Nuevo pago" manual. Si YaPagado, el dinero ya se recibio: el API registra
// el cobro y su abono completo en una sola operacion.
public sealed class RegistrarPagoViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante.")]
    public int IdEstudiante { get; set; }

    [Required(ErrorMessage = "Indique el concepto.")]
    [StringLength(150, ErrorMessage = "El concepto no puede pasar de 150 caracteres.")]
    public string Concepto { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal Monto { get; set; }

    public DateOnly? FechaEmision { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden pasar de 500 caracteres.")]
    public string? Observaciones { get; set; }

    public bool YaPagado { get; set; }

    public int? IdEncargadoPagador { get; set; }

    public int? IdMetodoPago { get; set; }

    [StringLength(100, ErrorMessage = "La referencia no puede pasar de 100 caracteres.")]
    public string? Referencia { get; set; }
}

// Pago (total o parcial) sobre un cobro que ya existe
public sealed class RegistrarAbonoViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione quién pagó.")]
    public int IdEncargadoPagador { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el método de pago.")]
    public int IdMetodoPago { get; set; }

    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal Monto { get; set; }

    public DateOnly? FechaAbono { get; set; }

    [StringLength(100, ErrorMessage = "La referencia no puede pasar de 100 caracteres.")]
    public string? Referencia { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden pasar de 500 caracteres.")]
    public string? Observaciones { get; set; }
}
