namespace FarmaGest.Dominio;

public class MedioPago
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public bool Estado { get; set; } = true;

    public List<Facturacion> Facturaciones { get; set; } = new();
}