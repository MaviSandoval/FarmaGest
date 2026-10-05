using FarmaGest.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Consulta de stock (Producto.stockVenta). El stock se modifica desde Productos
/// (ProductoService) y las ventas lo descuentan desde VentaService.
/// </summary>
public class StockService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public StockService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<StockProductoDto>> ObtenerStockAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Productos
            .AsNoTracking()
            .Where(p => p.Estado)
            .OrderBy(p => p.Descripcion)
            .Select(p => new StockProductoDto
            {
                Id = p.Id,
                Descripcion = p.Descripcion,
                Categoria = p.Categoria.Nombre,
                Stock = p.StockVenta,
                EsMedicamento = p.EsMedicamento
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
    public bool EsMedicamento { get; set; }

    public string Estado => EstadosStock.Calcular(Stock);
}
