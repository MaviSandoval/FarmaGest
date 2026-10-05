using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Ventas armadas por el Farmacéutico.
/// Flujo: el Farmacéutico arma la venta -> queda "Pendiente de cobro" ->
/// el Cajero la cobra (FacturacionService crea la Facturacion).
///
/// Stock: el Farmacéutico solo envía la preventa. Acá se verifica que haya stock,
/// pero NO se descuenta: el descuento corresponde al momento del cobro en caja.
/// </summary>
public class VentaService
{
    public const string EstadoPendiente = "Pendiente de cobro";
    public const string EstadoFacturada = "Facturada";
    public const string EstadoAnulada = "Anulada";

    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public VentaService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    // =========================================================
    // PRODUCTOS QUE SE PUEDEN VENDER
    // =========================================================
    public async Task<List<ProductoVendibleDto>> ObtenerProductosVendiblesAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Productos
            .AsNoTracking()
            .Where(p => p.Estado && p.StockVenta > 0)
            .OrderBy(p => p.Descripcion)
            .Select(p => new ProductoVendibleDto
            {
                Id = p.Id,
                Descripcion = p.Descripcion,
                Precio = p.PrecioVenta,
                RequiereReceta = p.RequiereReceta,
                EsMedicamento = p.EsMedicamento,
                Stock = p.StockVenta
            })
            .ToListAsync();
    }

    // =========================================================
    // CREAR VENTA (Farmacéutico -> caja)
    // =========================================================
    public async Task<int> CrearVentaAsync(int usuarioId, int? recetaId, IEnumerable<ItemVentaSolicitud> itemsSolicitados)
    {
        // Si un producto se cargó dos veces, se suman las cantidades
        var items = itemsSolicitados
            .GroupBy(i => i.ProductoId)
            .Select(g => new ItemVentaSolicitud(g.Key, g.Sum(i => i.Cantidad)))
            .ToList();

        if (items.Count == 0)
            throw new InvalidOperationException("Agregá al menos un producto a la venta.");
        if (items.Any(i => i.Cantidad <= 0))
            throw new InvalidOperationException("Las cantidades deben ser mayores a cero.");

        await using var context = await _factory.CreateDbContextAsync();
        await using var transaccion = await context.Database.BeginTransactionAsync();

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        // ---------- 1. Receta (opcional) ----------
        Receta? receta = null;

        if (recetaId != null)
        {
            receta = await context.Recetas
                .Include(r => r.Detalles)
                .Include(r => r.Afiliado).ThenInclude(a => a.Plan).ThenInclude(p => p.Coberturas)
                .FirstOrDefaultAsync(r => r.Id == recetaId)
                ?? throw new InvalidOperationException("La receta no existe.");

            if (receta.Estado != true)
                throw new InvalidOperationException("Solo se puede vender con una receta validada.");
            if (receta.FechaVencimiento < hoy)
                throw new InvalidOperationException("La receta está vencida.");
            if (!receta.Afiliado.Activo)
                throw new InvalidOperationException("El afiliado no está activo.");
            if (await context.Ventas.AnyAsync(v => v.RecetaId == recetaId && v.Estado))
                throw new InvalidOperationException("La receta ya se usó en otra venta.");
        }

        // ---------- 2. Productos ----------
        var ids = items.Select(i => i.ProductoId).ToList();
        var productos = await context.Productos
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var venta = new Venta
        {
            Fecha = DateTime.Now,
            Estado = true,
            TipoVenta = receta != null ? Venta.TipoConReceta : Venta.TipoLibre,
            RecetaId = receta?.Id,
            UsuarioId = usuarioId
        };

        foreach (var item in items)
        {
            if (!productos.TryGetValue(item.ProductoId, out var producto) || !producto.Estado)
                throw new InvalidOperationException("Uno de los productos no existe o está dado de baja.");

            var recetado = receta?.Detalles.FirstOrDefault(d => d.ProductoId == producto.Id);

            if (producto.RequiereReceta && recetado == null)
                throw new InvalidOperationException(
                    receta == null
                        ? $"\"{producto.Descripcion}\" requiere receta: no se puede vender como venta libre."
                        : $"\"{producto.Descripcion}\" requiere receta y no figura en la receta elegida.");

            if (recetado != null && item.Cantidad > recetado.Cantidad)
                throw new InvalidOperationException(
                    $"\"{producto.Descripcion}\": la receta autoriza {recetado.Cantidad} unidad(es).");

            if (item.Cantidad > producto.StockVenta)
                throw new InvalidOperationException(
                    $"\"{producto.Descripcion}\": stock insuficiente (hay {producto.StockVenta}).");

            // Cobertura: solo para lo recetado y si el plan cubre el producto
            CoberturaPlan? cobertura = recetado == null
                ? null
                : receta!.Afiliado.Plan.Coberturas.FirstOrDefault(c => c.ProductoId == producto.Id);

            decimal porcentaje = cobertura?.PorcentajeCobertura ?? 0;
            decimal descuento = Math.Round(item.Cantidad * producto.PrecioVenta * porcentaje / 100m, 2);

            venta.Detalles.Add(new DetalleVenta
            {
                Cantidad = item.Cantidad,
                PrecioUnitario = producto.PrecioVenta,
                Descuento = descuento,
                ProductoId = producto.Id,
                CoberturaId = cobertura?.Id
            });

            
        }

        context.Ventas.Add(venta);
        await context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return venta.Id;
    }

    // =========================================================
    // ANULAR (solo mientras no se cobró)
    // =========================================================
    public async Task AnularVentaAsync(int ventaId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        await using var transaccion = await context.Database.BeginTransactionAsync();

        var venta = await context.Ventas
            .FirstOrDefaultAsync(v => v.Id == ventaId)
            ?? throw new InvalidOperationException("La venta no existe.");

        if (!venta.Estado)
            throw new InvalidOperationException("La venta ya estaba anulada.");
        if (await context.Facturaciones.AnyAsync(f => f.VentaId == ventaId))
            throw new InvalidOperationException("La venta ya fue cobrada: no se puede anular.");

        // La preventa no descontó stock, así que no hay nada que devolver
        venta.Estado = false;

        await context.SaveChangesAsync();
        await transaccion.CommitAsync();
    }

    // =========================================================
    // CONSULTAS
    // =========================================================

    /// <summary>Ventas del período. Si se pasa usuarioId, solo las que armó ese usuario.</summary>
    public async Task<List<VentaResumenDto>> ObtenerVentasAsync(int? usuarioId = null, DateTime? desde = null, DateTime? hasta = null)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var consulta = context.Ventas.AsNoTracking();

        if (usuarioId != null)
            consulta = consulta.Where(v => v.UsuarioId == usuarioId);
        if (desde != null)
            consulta = consulta.Where(v => v.Fecha >= desde.Value.Date);
        if (hasta != null)
        {
            var finExclusivo = hasta.Value.Date.AddDays(1);
            consulta = consulta.Where(v => v.Fecha < finExclusivo);
        }

        var filas = await consulta
            .OrderByDescending(v => v.Fecha)
            .Select(v => new
            {
                v.Id,
                v.Fecha,
                v.Estado,
                v.TipoVenta,
                v.RecetaId,
                Afiliado = v.Receta == null ? null : v.Receta.Afiliado.Nombre + " " + v.Receta.Afiliado.Apellido,
                ObraSocial = v.Receta == null ? null : v.Receta.Afiliado.Plan.ObraSocial.Nombre,
                Responsable = v.Usuario.Nombre + " " + v.Usuario.Apellido,
                Items = v.Detalles.Sum(d => (int?)d.Cantidad) ?? 0,
                Bruto = v.Detalles.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario)) ?? 0,
                Descuento = v.Detalles.Sum(d => (decimal?)d.Descuento) ?? 0,
                MetodoPago = context.Facturaciones.Where(f => f.VentaId == v.Id).Select(f => f.MetodoPago).FirstOrDefault(),
                Cajero = context.Facturaciones.Where(f => f.VentaId == v.Id)
                    .Select(f => f.Usuario.Nombre + " " + f.Usuario.Apellido).FirstOrDefault()
            })
            .ToListAsync();

        return filas.Select(f => new VentaResumenDto
        {
            Numero = f.Id,
            Fecha = f.Fecha,
            Tipo = f.TipoVenta,
            TieneReceta = f.RecetaId != null,
            Cliente = f.Afiliado == null ? "Consumidor final" : $"{f.Afiliado} ({f.ObraSocial})",
            ObraSocial = f.ObraSocial ?? "Sin obra social",
            Responsable = f.Responsable,
            Items = f.Items,
            Subtotal = f.Bruto,
            Descuento = f.Descuento,
            Total = f.Bruto - f.Descuento,
            MedioPago = f.MetodoPago ?? "—",
            Cajero = f.Cajero ?? "—",
            Estado = !f.Estado ? EstadoAnulada : f.MetodoPago != null ? EstadoFacturada : EstadoPendiente
        }).ToList();
    }

    public async Task<List<DetalleVentaDto>> ObtenerDetalleAsync(int ventaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.DetallesVenta
            .AsNoTracking()
            .Where(d => d.VentaId == ventaId)
            .OrderBy(d => d.Id)
            .Select(d => new DetalleVentaDto
            {
                Producto = d.Producto.Descripcion,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Descuento = d.Descuento
            })
            .ToListAsync();
    }
}

// =========================================================
// DTOs
// =========================================================

public record ItemVentaSolicitud(int ProductoId, int Cantidad);

public class ProductoVendibleDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public bool RequiereReceta { get; set; }
    public bool EsMedicamento { get; set; }

    public string TextoCombo => $"{Descripcion} — $ {Precio:N2} (stock {Stock}){(RequiereReceta ? " · con receta" : "")}";
    public override string ToString() => Descripcion;
}

public class VentaResumenDto
{
    public int Numero { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public bool TieneReceta { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string ObraSocial { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public int Items { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string MedioPago { get; set; } = "—";
    public string Cajero { get; set; } = "—";
    public string Estado { get; set; } = string.Empty;

    public string Hora => Fecha.ToString("HH:mm");
    public bool PuedeAnular => Estado == VentaService.EstadoPendiente;
    public bool TieneTicket => Estado == VentaService.EstadoFacturada;
}

public class DetalleVentaDto
{
    public string Producto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario - Descuento;
}
