namespace FarmaGest.Dominio;

public class DetallePagoTarjeta
{
    public int Id { get; set; }

    public int FacturacionId { get; set; }
    public Facturacion Facturacion { get; set; } = null!;

    public string Ultimos4 { get; set; } = string.Empty;

    public string Titular { get; set; } = string.Empty;

    public string NumeroAutorizacion { get; set; } = string.Empty;

    public int Cuotas { get; set; } = 1;
}