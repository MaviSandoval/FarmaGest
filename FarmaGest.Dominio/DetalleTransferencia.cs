namespace FarmaGest.Dominio;

public class DetalleTransferencia
{
    public int Id { get; set; }

    public int FacturacionId { get; set; }
    public Facturacion Facturacion { get; set; } = null!;

    public string NumeroOperacion { get; set; } = string.Empty;
}