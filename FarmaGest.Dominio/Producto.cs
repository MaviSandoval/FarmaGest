namespace FarmaGest.Dominio;

public class Producto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal PrecioVenta { get; set; }
    public int StockVenta { get; set; }
    public bool RequiereReceta { get; set; }
    public bool EsMedicamento { get; set; }
    public bool Estado { get; set; } = true;

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
}