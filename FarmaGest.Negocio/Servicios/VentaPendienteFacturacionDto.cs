namespace FarmaGest.Negocio.Servicios;

public class VentaPendienteFacturacionDto
{
    public int VentaId { get; set; }

    public DateTime Fecha { get; set; }

    public string Responsable { get; set; } = string.Empty;

    public int CantidadProductos { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; } = "Pendiente de facturación";

    // Propiedades preparadas para la grilla actual
    public string Hora => Fecha.ToString("HH:mm");

    public string NumeroVenta => VentaId.ToString();
}
