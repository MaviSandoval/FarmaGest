using System.Globalization;
using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
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
                Etiqueta = porMes
                    ? cultura.TextInfo.ToTitleCase(g.Key.ToString("MMM yy", cultura))
                    : g.Key.ToString("dd/MM"),
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

    // =========================================================
    // RESUMEN DEL PERÍODO (tablero de arriba de Reportes)
    // =========================================================

    /// <summary>
    /// Indicadores y rankings del período, comparados con el período anterior de igual duración.
    /// Cruza Venta, DetalleVenta, Facturacion, Producto, Categoria, Receta, Afiliado, Plan y ObraSocial.
    /// </summary>
    public async Task<ResumenReportesDto> ResumenAsync(DateTime desde, DateTime hasta)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);
        int dias = Math.Max(1, (fin - inicio).Days);
        var cultura = new CultureInfo("es-AR");

        // ---------- Ventas cobradas del período ----------
        var ventas = await VentasCobradas(context, inicio, fin)
            .Select(v => new
            {
                v.Fecha,
                ConReceta = v.RecetaId != null,
                ObraSocial = v.Receta == null ? null : v.Receta.Afiliado.Plan.ObraSocial.Nombre,
                Bruto = v.Detalles.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario)) ?? 0,
                Descuento = v.Detalles.Sum(d => (decimal?)d.Descuento) ?? 0,
                Unidades = v.Detalles.Sum(d => (int?)d.Cantidad) ?? 0
            })
            .ToListAsync();

        decimal anterior = await VentasCobradas(context, inicio.AddDays(-dias), inicio)
            .SelectMany(v => v.Detalles)
            .SumAsync(d => (decimal?)(d.Cantidad * d.PrecioUnitario - d.Descuento)) ?? 0;

        var resumen = new ResumenReportesDto
        {
            Facturacion = ventas.Sum(v => v.Bruto - v.Descuento),
            FacturacionAnterior = anterior,
            Ventas = ventas.Count,
            Unidades = ventas.Sum(v => v.Unidades),
            UnidadesConReceta = ventas.Where(v => v.ConReceta).Sum(v => v.Unidades),
            VentasConObraSocial = ventas.Count(v => v.ConReceta),
            CoberturaDescontada = ventas.Sum(v => v.Descuento)
        };

        // ---------- Evolución: por día hasta 31 días, por semana si es más largo ----------
        if (dias <= 31)
        {
            var porDia = ventas.GroupBy(v => v.Fecha.Date).ToDictionary(g => g.Key, g => g.Sum(v => v.Bruto - v.Descuento));
            resumen.TituloEvolucion = "por día";
            for (var d = inicio; d < fin; d = d.AddDays(1))
            {
                string etiqueta = d == DateTime.Today ? "Hoy"
                    : dias <= 7 ? cultura.TextInfo.ToTitleCase(d.ToString("ddd d", cultura))
                    : d.ToString("dd/MM");
                resumen.Evolucion.Add(new PuntoEvolucionDto { Etiqueta = etiqueta, Total = porDia.GetValueOrDefault(d) });
            }
        }
        else
        {
            resumen.TituloEvolucion = "por semana";
            resumen.Evolucion = ventas
                .GroupBy(v => InicioSemana(v.Fecha))
                .OrderBy(g => g.Key)
                .Select(g => new PuntoEvolucionDto { Etiqueta = g.Key.ToString("dd/MM"), Total = g.Sum(v => v.Bruto - v.Descuento) })
                .ToList();
        }

        // ---------- Facturación por obra social (las 4 primeras + "Otras") ----------
        var porObraSocial = ventas
            .GroupBy(v => v.ObraSocial ?? "Particular")
            .Select(g => (Nombre: g.Key, Cantidad: g.Count(), Importe: g.Sum(v => v.Bruto)))
            .OrderByDescending(x => x.Importe)
            .ToList();

        if (porObraSocial.Count > 5)
        {
            var resto = porObraSocial.Skip(4).ToList();
            porObraSocial = porObraSocial.Take(4)
                .Append(("Otras", resto.Sum(x => x.Cantidad), resto.Sum(x => x.Importe)))
                .ToList();
        }
        resumen.ObrasSociales = Participaciones(porObraSocial);

        // ---------- Productos y categorías ----------
        var detalles = await VentasCobradas(context, inicio, fin)
            .SelectMany(v => v.Detalles)
            .GroupBy(d => new { d.ProductoId, d.Producto.Descripcion, Categoria = d.Producto.Categoria.Nombre })
            .Select(g => new
            {
                g.Key.Descripcion,
                g.Key.Categoria,
                Unidades = g.Sum(d => d.Cantidad),
                Total = g.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento)
            })
            .ToListAsync();

        resumen.TopProductos = detalles
            .OrderByDescending(d => d.Unidades)
            .Take(5)
            .Select((d, i) => new ReporteProductoDto
            {
                Puesto = i + 1,
                Producto = d.Descripcion,
                Categoria = d.Categoria,
                Unidades = d.Unidades,
                Total = d.Total
            })
            .ToList();

        resumen.Categorias = Participaciones(detalles
            .GroupBy(d => d.Categoria)
            .Select(g => (Nombre: g.Key, Cantidad: g.Sum(d => d.Unidades), Importe: g.Sum(d => d.Total)))
            .OrderByDescending(x => x.Importe)
            .ToList());

        // ---------- Medios de pago ----------
        var medios = await context.Facturaciones
            .AsNoTracking()
            .Where(f => f.Venta.Estado && f.Venta.Fecha >= inicio && f.Venta.Fecha < fin)
            .GroupBy(f => f.MetodoPago)
            .Select(g => new { Medio = g.Key, Cantidad = g.Count(), Importe = g.Sum(f => f.Importe) })
            .ToListAsync();

        resumen.MediosPago = Participaciones(medios
            .Select(m => (Nombre: m.Medio, m.Cantidad, m.Importe))
            .OrderByDescending(x => x.Importe)
            .ToList());

        return resumen;
    }

    /// <summary>
    /// Productos más vendidos dentro de cada obra social ("Particular" = ventas sin receta).
    /// Cruza Venta, DetalleVenta, Producto, Facturacion, Receta, Afiliado, Plan y ObraSocial.
    /// </summary>
    public async Task<List<ReporteProductoObraSocialDto>> ProductosPorObraSocialAsync(DateTime desde, DateTime hasta, int topPorObra = 5)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var (inicio, fin) = Rango(desde, hasta);

        var filas = await VentasCobradas(context, inicio, fin)
            .SelectMany(v => v.Detalles.Select(d => new
            {
                ObraSocial = v.Receta == null ? null : v.Receta.Afiliado.Plan.ObraSocial.Nombre,
                d.Producto.Descripcion,
                d.Cantidad,
                Bruto = d.Cantidad * d.PrecioUnitario,
                d.Descuento
            }))
            .GroupBy(x => new { x.ObraSocial, x.Descripcion })
            .Select(g => new
            {
                g.Key.ObraSocial,
                g.Key.Descripcion,
                Unidades = g.Sum(x => x.Cantidad),
                Bruto = g.Sum(x => x.Bruto),
                Cubierto = g.Sum(x => x.Descuento)
            })
            .ToListAsync();

        // Obras sociales ordenadas por facturación; dentro de cada una, el top por unidades
        return filas
            .GroupBy(f => f.ObraSocial ?? "Particular")
            .OrderByDescending(g => g.Sum(f => f.Bruto))
            .SelectMany(g => g
                .OrderByDescending(f => f.Unidades).ThenByDescending(f => f.Bruto)
                .Take(topPorObra)
                .Select((f, i) => new ReporteProductoObraSocialDto
                {
                    ObraSocial = g.Key,
                    Puesto = i + 1,
                    Producto = f.Descripcion,
                    Unidades = f.Unidades,
                    Cubierto = f.Cubierto,
                    Total = f.Bruto - f.Cubierto
                }))
            .ToList();
    }

    /// <summary>Ventas activas del rango que ya fueron cobradas (tienen Facturacion).</summary>
    private static IQueryable<Venta> VentasCobradas(FarmaGestDbContext context, DateTime inicio, DateTime fin) =>
        context.Ventas
            .AsNoTracking()
            .Where(v => v.Estado && v.Fecha >= inicio && v.Fecha < fin)
            .Where(v => context.Facturaciones.Any(f => f.VentaId == v.Id));

    private static List<ParticipacionDto> Participaciones(List<(string Nombre, int Cantidad, decimal Importe)> filas)
    {
        decimal total = filas.Sum(f => f.Importe);
        return filas.Select(f => new ParticipacionDto
        {
            Nombre = f.Nombre,
            Cantidad = f.Cantidad,
            Importe = f.Importe,
            Porcentaje = total == 0 ? 0 : Math.Round(f.Importe * 100 / total, 1)
        }).ToList();
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

public class ReporteProductoObraSocialDto
{
    public string ObraSocial { get; set; } = string.Empty;
    public int Puesto { get; set; }
    public string Producto { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal Cubierto { get; set; }
    public decimal Total { get; set; }
}

public class ResumenReportesDto
{
    public decimal Facturacion { get; set; }
    public decimal FacturacionAnterior { get; set; }
    public int Ventas { get; set; }
    public int Unidades { get; set; }
    public int UnidadesConReceta { get; set; }
    public int VentasConObraSocial { get; set; }
    public decimal CoberturaDescontada { get; set; }

    public string TituloEvolucion { get; set; } = string.Empty;
    public List<PuntoEvolucionDto> Evolucion { get; set; } = new();
    public List<ParticipacionDto> ObrasSociales { get; set; } = new();
    public List<ReporteProductoDto> TopProductos { get; set; } = new();
    public List<ParticipacionDto> Categorias { get; set; } = new();
    public List<ParticipacionDto> MediosPago { get; set; } = new();

    public decimal TicketPromedio => Ventas == 0 ? 0 : Math.Round(Facturacion / Ventas, 2);

    /// <summary>Variación % contra el período anterior (null si antes no hubo ventas).</summary>
    public decimal? Variacion => FacturacionAnterior == 0
        ? null
        : Math.Round((Facturacion - FacturacionAnterior) * 100 / FacturacionAnterior, 1);

    public decimal PorcentajeConObraSocial => Ventas == 0 ? 0 : Math.Round(VentasConObraSocial * 100m / Ventas, 0);
}

public class PuntoEvolucionDto
{
    public string Etiqueta { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class ParticipacionDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Importe { get; set; }
    public decimal Porcentaje { get; set; }

    public string Detalle => $"{Porcentaje:N0} %  ·  $ {Importe:N0}";
    public string PorcentajeTexto => $"{Porcentaje:N0} %";
}

public class ReportePeriodoDto
{
    public string Periodo { get; set; } = string.Empty;

    /// <summary>Texto corto para el eje del gráfico ("Oct 26" o "13/10").</summary>
    public string Etiqueta { get; set; } = string.Empty;
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
