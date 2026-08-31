namespace FarmaGest.Dominio;

public class Receta
{
    public int Id { get; set; }
    public DateOnly FechaEmision { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public string MatriculaMedico { get; set; } = string.Empty;
    public string NombreMedico { get; set; } = string.Empty;
    public bool Estado { get; set; } = true;

    public int AfiliadoId { get; set; }
    public Afiliado Afiliado { get; set; } = null!;

    public List<DetalleReceta> Detalles { get; set; } = new();
}

public class DetalleReceta
{
    public int Id { get; set; }
    public int Cantidad { get; set; }

    public int RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
}