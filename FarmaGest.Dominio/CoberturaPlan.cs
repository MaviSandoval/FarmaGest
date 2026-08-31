namespace FarmaGest.Dominio;

public class CoberturaPlan
{
    public int Id { get; set; }
    public decimal PorcentajeCobertura { get; set; }

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
}