using FarmaGest.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>Reportes del Administrador. Las ventas anuladas no suman en los totales.</summary>
public class ReporteService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public ReporteService(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    /// <param name="usuarioId">Si se indica, solo las ventas que armó ese usuario (reporte del Farmacéutico).</param>
    public async Task<List<ReporteObraSocialDto>> VentasPorObraSocialAsync(DateTime desde, DateTime hasta, int? usuarioId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);

        var ventas = await context.Ventas
            .AsNoTracking()
            .Where(v => v.Estado && v.Fecha >= inicio && v.Fecha < fin)
            .Where(v => usuarioId == null || v.UsuarioId == usuarioId)
            .Select(v => new
            {
                ObraSocial = v.Receta == null ? null : v.Receta.Afiliado.Plan.ObraSocial.Nombre,
                Bruto = v.Detalles.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario)) ?? 0,
                Descuento = v.Detalles.Sum(d => (decimal?)d.Descuento) ?? 0
            })
            .ToListAsync();

        return ventas
            .GroupBy(v => v.ObraSocial ?? "Sin obra social")
            .Select(g => new ReporteObraSocialDto
            {
                ObraSocial = g.Key,
                Ventas = g.Count(),
                TotalVendido = g.Sum(v => v.Bruto),
                TotalCubierto = g.Sum(v => v.Descuento)
            })
            .OrderByDescending(r => r.TotalVendido)
            .ToList();
    }

    /// <param name="usuarioId">Si se indica, solo las ventas que armó ese usuario (reporte del Farmacéutico).</param>
    public async Task<List<ReporteProductoDto>> ProductosMasVendidosAsync(DateTime desde, DateTime hasta, int top = 20, int? usuarioId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);

        var filas = await context.DetallesVenta
            .AsNoTracking()
            .Where(d => d.Venta.Estado && d.Venta.Fecha >= inicio && d.Venta.Fecha < fin)
            .Where(d => usuarioId == null || d.Venta.UsuarioId == usuarioId)
            .GroupBy(d => new { d.ProductoId, d.Producto.Descripcion, Categoria = d.Producto.Categoria.Nombre })
            .Select(g => new
            {
                g.Key.Descripcion,
                g.Key.Categoria,
                Unidades = g.Sum(d => d.Cantidad),
                Total = g.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento)
            })
            .OrderByDescending(x => x.Unidades)
            .Take(top)
            .ToListAsync();

        return filas.Select((f, i) => new ReporteProductoDto
        {
            Puesto = i + 1,
            Producto = f.Descripcion,
            Categoria = f.Categoria,
            Unidades = f.Unidades,
            Total = f.Total
        }).ToList();
    }

    public async Task<List<ReporteCierreDto>> CierresDeCajaAsync(DateTime desde, DateTime hasta)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);

        var cajas = await context.Cajas
            .AsNoTracking()
            .Where(c => !c.Estado && c.FechaCierre != null && c.FechaCierre >= inicio && c.FechaCierre < fin)
            .OrderByDescending(c => c.FechaCierre)
            .Select(c => new
            {
                c.Id,
                c.FechaCierre,
                Cajero = c.Usuario.Nombre + " " + c.Usuario.Apellido,
                c.MontoInicial,
                c.MontoFinal,
                // Mismo criterio que CajaService: monto inicial + cobros de ventas no anuladas
                Cobrado = context.Facturaciones
                    .Where(f => f.CajaId == c.Id && f.Venta.Estado)
                    .Sum(f => (decimal?)f.Importe) ?? 0
            })
            .ToListAsync();

        return cajas.Select(c => new ReporteCierreDto
        {
            Fecha = c.FechaCierre!.Value.ToString("dd/MM/yyyy HH:mm"),
            Caja = c.Id,
            Cajero = c.Cajero,
            Esperado = c.MontoInicial + c.Cobrado,
            Contado = c.MontoFinal ?? 0
        }).ToList();
    }

    public async Task<List<ReporteFarmaceuticoDto>> VentasPorFarmaceuticoAsync(DateTime desde, DateTime hasta)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);

        var ventas = await context.Ventas
            .AsNoTracking()
            .Where(v => v.Fecha >= inicio && v.Fecha < fin)
            .Select(v => new
            {
                v.UsuarioId,
                Farmaceutico = v.Usuario.Nombre + " " + v.Usuario.Apellido,
                v.Estado,
                ConReceta = v.RecetaId != null,
                Total = v.Detalles.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario - d.Descuento)) ?? 0
            })
            .ToListAsync();

        return ventas
            .GroupBy(v => new { v.UsuarioId, v.Farmaceutico })
            .Select(g => new ReporteFarmaceuticoDto
            {
                Farmaceutico = g.Key.Farmaceutico,
                Ventas = g.Count(v => v.Estado),
                ConReceta = g.Count(v => v.Estado && v.ConReceta),
                Anuladas = g.Count(v => !v.Estado),
                Total = g.Where(v => v.Estado).Sum(v => v.Total)
            })
            .OrderByDescending(r => r.Total)
            .ToList();
    }

    /// <summary>
    /// Recetas por obra social y estado, según la fecha de emisión.
    /// Cruza Recetas, Afiliados, Planes, ObraSocial y Venta (para saber cuáles se usaron).
    /// </summary>
    public async Task<List<ReporteRecetasDto>> RecetasPorObraSocialAsync(DateTime desde, DateTime hasta)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var inicio = DateOnly.FromDateTime(desde);
        var fin = DateOnly.FromDateTime(hasta);

        var recetas = await context.Recetas
            .AsNoTracking()
            .Where(r => r.FechaEmision >= inicio && r.FechaEmision <= fin)
            .Select(r => new
            {
                ObraSocial = r.Afiliado.Plan.ObraSocial.Nombre,
                r.Estado,
                Usada = context.Ventas.Any(v => v.RecetaId == r.Id && v.Estado)
            })
            .ToListAsync();

        return recetas
            .GroupBy(r => r.ObraSocial)
            .Select(g => new ReporteRecetasDto
            {
                ObraSocial = g.Key,
                Total = g.Count(),
                Pendientes = g.Count(r => r.Estado == null),
                Validadas = g.Count(r => r.Estado == true),
                Rechazadas = g.Count(r => r.Estado == false),
                Usadas = g.Count(r => r.Usada)
            })
            .OrderByDescending(r => r.Total)
            .ToList();
    }

    // "hasta" es inclusivo: se toma hasta el final de ese día
    private static (DateTime Inicio, DateTime Fin) Rango(DateTime desde, DateTime hasta) =>
        (desde.Date, hasta.Date.AddDays(1));
}

public class ReporteObraSocialDto
{
    public string ObraSocial { get; set; } = string.Empty;
    public int Ventas { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal TotalCubierto { get; set; }
    public decimal PagadoCliente => TotalVendido - TotalCubierto;
}

public class ReporteRecetasDto
{
    public string ObraSocial { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Pendientes { get; set; }
    public int Validadas { get; set; }
    public int Rechazadas { get; set; }
    public int Usadas { get; set; }
}

public class ReporteProductoDto
{
    public int Puesto { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal Total { get; set; }
}

public class ReporteCierreDto
{
    public string Fecha { get; set; } = string.Empty;
    public int Caja { get; set; }
    public string Cajero { get; set; } = string.Empty;
    public decimal Esperado { get; set; }
    public decimal Contado { get; set; }
    public decimal Diferencia => Contado - Esperado;

    // Verde si el arqueo cerró justo, rojo si hubo sobrante o faltante
    public string Estado => Diferencia == 0 ? "Sin diferencia" : "Con diferencia";
}

public class ReporteFarmaceuticoDto
{
    public string Farmaceutico { get; set; } = string.Empty;
    public int Ventas { get; set; }
    public int ConReceta { get; set; }
    public int Anuladas { get; set; }
    public decimal Total { get; set; }
}
