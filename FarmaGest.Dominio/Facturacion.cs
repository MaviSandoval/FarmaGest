namespace FarmaGest.Dominio;

public class Facturacion
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    public string MetodoPago { get; set; } = string.Empty;

    public decimal Importe { get; set; }

    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int CajaId { get; set; }
    public Caja Caja { get; set; } = null!;
}