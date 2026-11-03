namespace FarmaGest.Dominio;

public class Facturacion
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    // Campo anterior.
    // Se mantiene temporalmente hasta terminar la migración.
    public string MetodoPago { get; set; } = string.Empty;

    public decimal Importe { get; set; }

    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;

    public decimal MontoCambio { get; set; }

    // Medio de pago normalizado
    public int MedioPagoId { get; set; }
    public MedioPago MedioPago { get; set; } = null!;

    // Detalle opcional para pagos con Débito o Crédito
    public DetallePagoTarjeta? DetallePagoTarjeta { get; set; }

    // Detalle opcional para pagos por Transferencia
    public DetalleTransferencia? DetalleTransferencia { get; set; }
}