using System.ComponentModel.DataAnnotations;

namespace MenteActiva.Api.Models;

// "Nuevo pago" manual. Si YaPagado, el dinero ya se recibio: se registra el
// cobro y su abono completo en una sola operacion.
public sealed record RegistrarPagoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un estudiante.")]
    public int IdEstudiante { get; init; }

    [Required(ErrorMessage = "Indique el concepto.")]
    [StringLength(150, ErrorMessage = "El concepto no puede pasar de 150 caracteres.")]
    public string Concepto { get; init; } = string.Empty;

    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal Monto { get; init; }

    // Vacio = hoy
    public DateOnly? FechaEmision { get; init; }

    // Vacio = segun el plazo de la configuracion
    public DateOnly? FechaVencimiento { get; init; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden pasar de 500 caracteres.")]
    public string? Observaciones { get; init; }

    public bool YaPagado { get; init; }

    public int? IdEncargadoPagador { get; init; }

    public int? IdMetodoPago { get; init; }

    [StringLength(100, ErrorMessage = "La referencia no puede pasar de 100 caracteres.")]
    public string? Referencia { get; init; }
}

// Abono sobre un cobro que ya existe (pago parcial o total)
public sealed record RegistrarAbonoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione quién pagó.")]
    public int IdEncargadoPagador { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el método de pago.")]
    public int IdMetodoPago { get; init; }

    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal Monto { get; init; }

    // Vacio = hoy
    public DateOnly? FechaAbono { get; init; }

    [StringLength(100, ErrorMessage = "La referencia no puede pasar de 100 caracteres.")]
    public string? Referencia { get; init; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden pasar de 500 caracteres.")]
    public string? Observaciones { get; init; }
}

// Anular un cobro o un abono: siempre con motivo
public sealed record AnularPagoRequest
{
    [Required(ErrorMessage = "Indique el motivo de la anulación.")]
    [StringLength(300, ErrorMessage = "El motivo no puede pasar de 300 caracteres.")]
    public string Motivo { get; init; } = string.Empty;
}