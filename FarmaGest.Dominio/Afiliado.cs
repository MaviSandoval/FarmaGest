namespace FarmaGest.Dominio;

public class Afiliado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public List<Receta> Recetas { get; set; } = new();
}