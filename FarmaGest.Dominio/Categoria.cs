namespace FarmaGest.Dominio;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;

    public List<Producto> Productos { get; set; } = new();
}