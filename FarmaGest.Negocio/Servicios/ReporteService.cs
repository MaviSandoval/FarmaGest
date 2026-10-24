using FarmaGest.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Reportes cruzados. Una venta cuenta como vendida recién cuando el Cajero la cobró
/// (tiene Facturacion): las preventas pendientes de cobro y las anuladas no suman.
/// </summary>
public class ReporteService
{
    // Mismo texto que guarda el cobro en Facturacion.MetodoPago
    private const string MedioEfectivo = "Efectivo";

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
            .Where(v => context.Facturaciones.Any(f => f.VentaId == v.Id))
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
            .Where(d => context.Facturaciones.Any(f => f.VentaId == d.VentaId))
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

    /// <summary>
    /// Cajas abiertas en el período (abiertas y cerradas), con el arqueo.
    /// Mismo criterio que CajaService: el efectivo esperado es monto inicial + cobros en efectivo;
    /// los otros medios (débito, crédito, transferencia) no van al cajón.
    /// Cruza Caja, Usuario, Facturacion y Venta.
    /// </summary>
    public async Task<List<ReporteCierreDto>> CierresDeCajaAsync(DateTime desde, DateTime hasta)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);

        var cajas = await context.Cajas
            .AsNoTracking()
            .Where(c => c.FechaApertura >= inicio && c.FechaApertura < fin)
            .OrderByDescending(c => c.FechaApertura)
            .Select(c => new ReporteCierreDto
            {
                Caja = c.Id,
                Cajero = c.Usuario.Nombre + " " + c.Usuario.Apellido,
                Apertura = c.FechaApertura,
                Cierre = c.FechaCierre,
                Abierta = c.Estado,
                MontoInicial = c.MontoInicial,
                Contado = c.MontoFinal,
                Cobros = context.Facturaciones.Count(f => f.CajaId == c.Id && f.Venta.Estado),
                Efectivo = context.Facturaciones
                    .Where(f => f.CajaId == c.Id && f.Venta.Estado && f.MetodoPago == MedioEfectivo)
                    .Sum(f => (decimal?)f.Importe) ?? 0,
                OtrosMedios = context.Facturaciones
                    .Where(f => f.CajaId == c.Id && f.Venta.Estado && f.MetodoPago != MedioEfectivo)
                    .Sum(f => (decimal?)f.Importe) ?? 0
            })
            .ToListAsync();

        return cajas;
    }

    /// <summary>
    /// Cobros del período agrupados por medio de pago (según la fecha de cobro).
    /// Cruza Facturacion, Venta y Caja.
    /// </summary>
    public async Task<List<ReporteMedioPagoDto>> CobrosPorMedioDePagoAsync(DateTime desde, DateTime hasta)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);

        var filas = await context.Facturaciones
            .AsNoTracking()
            .Where(f => f.Venta.Estado && f.Fecha >= inicio && f.Fecha < fin)
            .GroupBy(f => f.MetodoPago)
            .Select(g => new
            {
                Medio = g.Key,
                Cobros = g.Count(),
                Cajas = g.Select(f => f.CajaId).Distinct().Count(),
                Importe = g.Sum(f => f.Importe)
            })
            .ToListAsync();

        decimal total = filas.Sum(f => f.Importe);

        return filas
            .OrderByDescending(f => f.Importe)
            .Select(f => new ReporteMedioPagoDto
            {
                Medio = f.Medio,
                Cobros = f.Cobros,
                Cajas = f.Cajas,
                Importe = f.Importe,
                Porcentaje = total == 0 ? 0 : Math.Round(f.Importe * 100 / total, 1)
            })
            .ToList();
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
                Facturada = context.Facturaciones.Any(f => f.VentaId == v.Id),
                ConReceta = v.RecetaId != null,
                Total = v.Detalles.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario - d.Descuento)) ?? 0
            })
            .ToListAsync();

        return ventas
            .GroupBy(v => new { v.UsuarioId, v.Farmaceutico })
            .Select(g => new ReporteFarmaceuticoDto
            {
                Farmaceutico = g.Key.Farmaceutico,
                // Ventas, con receta y total: solo las cobradas. Anuladas: todas las del período.
                Ventas = g.Count(v => v.Estado && v.Facturada),
                ConReceta = g.Count(v => v.Estado && v.Facturada && v.ConReceta),
                Anuladas = g.Count(v => !v.Estado),
                Total = g.Where(v => v.Estado && v.Facturada).Sum(v => v.Total)
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

    /// <summary>
    /// Ventas cobradas agrupadas por semana (lunes a domingo) o por mes.
    /// Cruza Venta, DetalleVenta, Facturacion y Receta.
    /// </summary>
    /// <param name="porMes">true agrupa por mes; false, por semana.</param>
    /// <param name="usuarioId">Si se indica, solo las ventas que armó ese usuario (reporte del Farmacéutico).</param>
    public async Task<List<ReportePeriodoDto>> VentasPorPeriodoAsync(DateTime desde, DateTime hasta, bool porMes, int? usuarioId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);

        var ventas = await context.Ventas
            .AsNoTracking()
            .Where(v => v.Estado && v.Fecha >= inicio && v.Fecha < fin)
            .Where(v => context.Facturaciones.Any(f => f.VentaId == v.Id))
            .Where(v => usuarioId == null || v.UsuarioId == usuarioId)
            .Select(v => new
            {
                v.Fecha,
                ConReceta = v.RecetaId != null,
                Bruto = v.Detalles.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario)) ?? 0,
                Descuento = v.Detalles.Sum(d => (decimal?)d.Descuento) ?? 0
            })
            .ToListAsync();

        var cultura = new System.Globalization.CultureInfo("es-AR");

        return ventas
            .GroupBy(v => porMes ? new DateTime(v.Fecha.Year, v.Fecha.Month, 1) : InicioSemana(v.Fecha))
            .OrderBy(g => g.Key)
            .Select(g => new ReportePeriodoDto
            {
                Periodo = porMes
                    ? cultura.TextInfo.ToTitleCase(g.Key.ToString("MMMM yyyy", cultura))
                    : $"Semana del {g.Key:dd/MM} al {g.Key.AddDays(6):dd/MM/yyyy}",
                Ventas = g.Count(),
                ConReceta = g.Count(v => v.ConReceta),
                TotalVendido = g.Sum(v => v.Bruto),
                TotalCubierto = g.Sum(v => v.Descuento)
            })
            .ToList();
    }

    // La semana empieza el lunes
    private static DateTime InicioSemana(DateTime fecha) =>
        fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

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

public class ReportePeriodoDto
{
    public string Periodo { get; set; } = string.Empty;
    public int Ventas { get; set; }
    public int ConReceta { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal TotalCubierto { get; set; }
    public decimal PagadoCliente => TotalVendido - TotalCubierto;
    public decimal TicketPromedio => Ventas == 0 ? 0 : Math.Round(PagadoCliente / Ventas, 2);
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
    public int Caja { get; set; }
    public string Cajero { get; set; } = string.Empty;
    public DateTime Apertura { get; set; }
    public DateTime? Cierre { get; set; }
    public bool Abierta { get; set; }
    public decimal MontoInicial { get; set; }
    public int Cobros { get; set; }
    public decimal Efectivo { get; set; }
    public decimal OtrosMedios { get; set; }
    public decimal? Contado { get; set; }

    public decimal TotalCobrado => Efectivo + OtrosMedios;
    public decimal Esperado => MontoInicial + Efectivo;

    // Sin diferencia mientras la caja sigue abierta (todavía no se contó)
    public decimal? Diferencia => Abierta || Contado == null ? null : Contado - Esperado;

    public string AperturaTexto => Apertura.ToString("dd/MM/yyyy HH:mm");
    public string CierreTexto => Cierre?.ToString("dd/MM/yyyy HH:mm") ?? "—";

    public string Estado => Abierta ? "Abierta" : Diferencia == 0 ? "Sin diferencia" : "Con diferencia";
}

public class ReporteMedioPagoDto
{
    public string Medio { get; set; } = string.Empty;
    public int Cobros { get; set; }
    public int Cajas { get; set; }
    public decimal Importe { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal TicketPromedio => Cobros == 0 ? 0 : Math.Round(Importe / Cobros, 2);
}

public class ReporteFarmaceuticoDto
{
    public string Farmaceutico { get; set; } = string.Empty;
    public int Ventas { get; set; }
    public int ConReceta { get; set; }
    public int Anuladas { get; set; }
    public decimal Total { get; set; }
}
