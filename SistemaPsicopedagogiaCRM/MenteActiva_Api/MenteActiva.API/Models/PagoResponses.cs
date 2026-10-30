namespace MenteActiva.Api.Models;

// Una fila de la tabla de Pagos (sale de SP_Pago_Listar)
public sealed record PagoListaResponse
{
    public int IdPago { get; init; }
    public int IdEstudiante { get; init; }
    public string Estudiante { get; init; } = string.Empty;
    public string? Encargado { get; init; }
    public string Concepto { get; init; } = string.Empty;
    public decimal Monto { get; init; }
    public decimal Abonado { get; init; }
    public decimal Saldo { get; init; }
    public DateTime FechaEmision { get; init; }
    public DateTime? FechaVencimiento { get; init; }
    public int IdEstadoPago { get; init; }
    public string Estado { get; init; } = string.Empty;
    public int? IdCita { get; init; }
    public string? MetodoPago { get; init; }

    // Cobro creado al completarse una cita, o registrado a mano
    public string Origen => IdCita is null ? "Manual" : "Cita";
}

// Lo que la pantalla necesita al abrir: los cobros y que tipos de sesion no
// tienen precio (sus citas no generan cobro y conviene avisarlo)
public sealed record PagosPantallaResponse(
    IReadOnlyList<PagoListaResponse> Pagos,
    IReadOnlyList<string> TiposSesionSinPrecio);

public sealed record PagoEncabezadoResponse
{
    public int IdPago { get; init; }
    public int IdEstudiante { get; init; }
    public string Estudiante { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public decimal Monto { get; init; }
    public decimal Abonado { get; init; }
    public decimal Saldo { get; init; }
    public DateTime FechaEmision { get; init; }
    public DateTime? FechaVencimiento { get; init; }
    public int IdEstadoPago { get; init; }
    public string Estado { get; init; } = string.Empty;
    public int? IdCita { get; init; }
    public DateTime? FechaCita { get; init; }
    public string? Observaciones { get; init; }
}

public sealed record AbonoResponse
{
    public int IdAbono { get; init; }
    public DateTime FechaAbono { get; init; }
    public decimal Monto { get; init; }
    public string MetodoPago { get; init; } = string.Empty;
    public string Pagador { get; init; } = string.Empty;
    public string? Referencia { get; init; }
    public string? Observaciones { get; init; }
    public bool Activo { get; init; }
}

public sealed record EncargadoPagadorResponse
{
    public int IdEncargado { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool EsPrincipal { get; init; }
}

public sealed record PagoDetalleResponse(
    PagoEncabezadoResponse Pago,
    IReadOnlyList<AbonoResponse> Abonos,
    IReadOnlyList<EncargadoPagadorResponse> Encargados);

public sealed record MetodoPagoResponse
{
    public int IdMetodoPago { get; init; }
    public string Nombre { get; init; } = string.Empty;
}

// Para el modal Nuevo pago: metodos y tipos de sesion (prellenan concepto y monto)
public sealed record CatalogosPagoResponse(
    IReadOnlyList<MetodoPagoResponse> Metodos,
    IReadOnlyList<TipoSesionResponse> TiposSesion);