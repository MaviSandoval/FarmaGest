using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class ProductoService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public ProductoService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Producto>> ObtenerTodosAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Productos.Include(p => p.Categoria).OrderBy(p => p.Descripcion).ToListAsync();
    }

    public async Task<List<Categoria>> ObtenerCategoriasAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Categorias.Where(c => c.Estado).OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task CrearAsync(string descripcion, decimal precioVenta, int stockVenta,
        bool requiereReceta, bool esMedicamento, int categoriaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var producto = new Producto
        {
            Descripcion = descripcion,
            PrecioVenta = precioVenta,
            StockVenta = stockVenta,
            RequiereReceta = requiereReceta,
            EsMedicamento = esMedicamento,
            CategoriaId = categoriaId,
            Estado = true
        };

        context.Productos.Add(producto);
        await context.SaveChangesAsync();
    }

    public async Task EditarAsync(int id, string descripcion, decimal precioVenta, int stockVenta,
        bool requiereReceta, bool esMedicamento, int categoriaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var producto = await context.Productos.FindAsync(id)
            ?? throw new KeyNotFoundException("Producto no encontrado.");

        producto.Descripcion = descripcion;
        producto.PrecioVenta = precioVenta;
        producto.StockVenta = stockVenta;
        producto.RequiereReceta = requiereReceta;
        producto.EsMedicamento = esMedicamento;
        producto.CategoriaId = categoriaId;

        await context.SaveChangesAsync();
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var producto = await context.Productos.FindAsync(id)
            ?? throw new KeyNotFoundException("Producto no encontrado.");

        producto.Estado = activo;
        await context.SaveChangesAsync();
    }
}