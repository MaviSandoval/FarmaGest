using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Catálogo de productos. El alta, la edición y la baja las hace el Farmacéutico;
/// el Administrador solo consulta.
/// </summary>
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
        return await context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .OrderBy(p => p.Descripcion)
            .ToListAsync();
    }

    public async Task<List<Categoria>> ObtenerCategoriasAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Categorias
            .AsNoTracking()
            .Where(c => c.Estado)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    /// <summary>Productos con su cobertura en cada plan de obra social.</summary>
    public async Task<List<ProductoCatalogoDto>> ObtenerCatalogoAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        var productos = await context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

        var coberturas = await context.CoberturaPlanes
            .AsNoTracking()
            .Select(c => new
            {
                c.ProductoId,
                Plan = c.Plan.ObraSocial.Nombre + " · " + c.Plan.NombrePlan,
                c.PorcentajeCobertura
            })
            .ToListAsync();

        return productos.Select(p => new ProductoCatalogoDto
        {
            Id = p.Id,
            Descripcion = p.Descripcion,
            CategoriaId = p.CategoriaId,
            Categoria = p.Categoria.Nombre,
            Precio = p.PrecioVenta,
            Stock = p.StockVenta,
            RequiereReceta = p.RequiereReceta,
            EsMedicamento = p.EsMedicamento,
            Activo = p.Estado,
            Coberturas = coberturas
                .Where(c => c.ProductoId == p.Id)
                .OrderBy(c => c.Plan)
                .Select(c => new CoberturaProductoDto { Plan = c.Plan, Porcentaje = c.PorcentajeCobertura })
                .ToList()
        }).ToList();
    }

    public async Task CrearAsync(string descripcion, decimal precioVenta, int stockVenta,
        bool requiereReceta, bool esMedicamento, int categoriaId)
    {
        descripcion = descripcion.Trim();
        ValidarDatos(descripcion, precioVenta, stockVenta);

        await using var context = await _factory.CreateDbContextAsync();

        if (await context.Productos.AnyAsync(p => p.Descripcion == descripcion))
            throw new InvalidOperationException("Ya existe un producto con esa descripción.");

        context.Productos.Add(new Producto
        {
            Descripcion = descripcion,
            PrecioVenta = precioVenta,
            StockVenta = stockVenta,
            RequiereReceta = requiereReceta,
            EsMedicamento = esMedicamento,
            CategoriaId = categoriaId,
            Estado = true
        });

        await context.SaveChangesAsync();
    }

    public async Task EditarAsync(int id, string descripcion, decimal precioVenta, int stockVenta,
        bool requiereReceta, bool esMedicamento, int categoriaId)
    {
        descripcion = descripcion.Trim();
        ValidarDatos(descripcion, precioVenta, stockVenta);

        await using var context = await _factory.CreateDbContextAsync();

        var producto = await context.Productos.FindAsync(id)
            ?? throw new InvalidOperationException("Producto no encontrado.");

        if (await context.Productos.AnyAsync(p => p.Id != id && p.Descripcion == descripcion))
            throw new InvalidOperationException("Ya existe otro producto con esa descripción.");

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
            ?? throw new InvalidOperationException("Producto no encontrado.");

        producto.Estado = activo;
        await context.SaveChangesAsync();
    }

    private static void ValidarDatos(string descripcion, decimal precioVenta, int stockVenta)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new InvalidOperationException("La descripción es obligatoria.");
        if (descripcion.Length > 150)
            throw new InvalidOperationException("La descripción admite hasta 150 caracteres.");
        if (precioVenta <= 0)
            throw new InvalidOperationException("El precio debe ser mayor a cero.");
        if (stockVenta < 0)
            throw new InvalidOperationException("El stock no puede ser negativo.");
        
    }
}

public class ProductoCatalogoDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public bool RequiereReceta { get; set; }
    public bool EsMedicamento { get; set; }
    public bool Activo { get; set; }
    public List<CoberturaProductoDto> Coberturas { get; set; } = new();

    public string RequiereRecetaTexto => RequiereReceta ? "Sí" : "No";
    public string TipoTexto => EsMedicamento ? "Medicamento" : "Perfumería / cuidado personal";
    public string Estado => Activo ? "Activo" : "Inactivo";
    public string EstadoStock => EstadosStock.Calcular(Stock);
    public string TextoAccionEstado => Activo ? "Dar de baja" : "Reactivar";
}

public class CoberturaProductoDto
{
    public string Plan { get; set; } = string.Empty;
    public decimal Porcentaje { get; set; }
}

/// <summary>
/// Regla única para clasificar el stock de un producto.
/// La base no tiene stock mínimo por producto: se usa un umbral general.
/// </summary>
public static class EstadosStock
{
    public const int UmbralStockBajo = 10;

    public const string Normal = "Normal";
    public const string Bajo = "Bajo";
    public const string SinStock = "Sin stock";

    public static string Calcular(int stock) =>
        stock <= 0 ? SinStock :
        stock < UmbralStockBajo ? Bajo : Normal;
}
