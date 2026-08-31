namespace FarmaGest.Dominio;

public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public bool Estado { get; set; } = true;
    public string TipoVenta { get; set; } = string.Empty;

    public int? RecetaId { get; set; }
    public Receta? Receta { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;

    public List<DetalleVenta> Detalles { get; set; } = new();
}

public class DetalleVenta
{
    public int Id { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public int? CoberturaId { get; set; }
    public CoberturaPlan? Cobertura { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario - Descuento;
}