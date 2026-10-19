using FarmaGest.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Consulta de stock (Producto.stockVenta). El stock se modifica desde Productos
/// (ProductoService) y las ventas lo reservan/descuentan desde VentaService al enviar la preventa.
/// </summary>
public class StockService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public StockService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    /// <param name="incluirBajas">true para traer también los productos dados de baja.</param>
    public async Task<List<StockProductoDto>> ObtenerStockAsync(bool incluirBajas = false)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Productos
            .AsNoTracking()
            .Where(p => incluirBajas || p.Estado)
            .OrderBy(p => p.Descripcion)
            .Select(p => new StockProductoDto
            {
                Id = p.Id,
                Descripcion = p.Descripcion,
                Categoria = p.Categoria.Nombre,
                Stock = p.StockVenta,
                Precio = p.PrecioVenta,
                RequiereReceta = p.RequiereReceta,
                EsMedicamento = p.EsMedicamento,
                Activo = p.Estado
            })
            .ToListAsync();
    }
}

public class StockProductoDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Precio { get; set; }
    public bool RequiereReceta { get; set; }
    public bool EsMedicamento { get; set; }
    public bool Activo { get; set; } = true;

    public string RequiereRecetaTexto => RequiereReceta ? "Sí" : "No";

    // Un producto dado de baja se muestra como "Inactivo"; el resto según su stock
    public string Estado => Activo ? EstadosStock.Calcular(Stock) : "Inactivo";
}
