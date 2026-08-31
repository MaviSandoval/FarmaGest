namespace FarmaGest.Dominio;

public class Plan
{
    public int Id { get; set; }
    public string NombrePlan { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public int ObraSocialId { get; set; }
    public ObraSocial ObraSocial { get; set; } = null!;

    public List<Afiliado> Afiliados { get; set; } = new();
    public List<CoberturaPlan> Coberturas { get; set; } = new();
}