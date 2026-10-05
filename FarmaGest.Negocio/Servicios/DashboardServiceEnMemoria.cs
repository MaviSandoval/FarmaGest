using System.Globalization;
using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

/// <summary>
/// Datos de la pantalla de Inicio del Administrador, leídos de la base.
/// (Se mantiene el nombre de la clase para no cambiar el registro en App.xaml.cs.)
/// Las ventas anuladas no suman.
/// </summary>
public class DashboardServiceEnMemoria : IDashboardService
{
    private readonly IDbContextFactory<FarmaGestDbContext> _factory;

    public DashboardServiceEnMemoria(IDbContextFactory<FarmaGestDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<DashboardResumen> ObtenerResumenAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);
        var ayer = hoy.AddDays(-1);
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);
        var mismoDiaMesAnterior = inicioMesAnterior.AddDays(hoy.Day); // mismo tramo del mes anterior
        var hace7Dias = hoy.AddDays(-6);

        // Total de cada venta activa desde el mes anterior (alcanza para todos los indicadores)
        var ventas = await context.Ventas
            .AsNoTracking()
            .Where(v => v.Estado && v.Fecha >= inicioMesAnterior && v.Fecha < manana)
            .Select(v => new
            {
                v.Fecha,
                ConReceta = v.RecetaId != null,
                Total = v.Detalles.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario - d.Descuento)) ?? 0
            })
            .ToListAsync();

        decimal totalHoy = ventas.Where(v => v.Fecha >= hoy).Sum(v => v.Total);
        decimal totalAyer = ventas.Where(v => v.Fecha >= ayer && v.Fecha < hoy).Sum(v => v.Total);
        decimal totalMes = ventas.Where(v => v.Fecha >= inicioMes).Sum(v => v.Total);
        decimal totalMesAnterior = ventas
            .Where(v => v.Fecha >= inicioMesAnterior && v.Fecha < mismoDiaMesAnterior)
            .Sum(v => v.Total);

        var cultura = new CultureInfo("es-AR");
        var puntosPorDia = Enumerable.Range(0, 7)
            .Select(offset => hace7Dias.AddDays(offset))
            .Select(dia => new PuntoVentaDiaria
            {
                Dia = dia.ToString("ddd", cultura),
                Total = ventas.Where(v => v.Fecha.Date == dia).Sum(v => v.Total)
            })
            .ToList();

        // Stock bajo: debajo del umbral general
        const int umbral = EstadosStock.UmbralStockBajo;
        var stockBajo = await context.Productos
            .AsNoTracking()
            .Where(p => p.Estado && p.StockVenta < umbral)
            .OrderBy(p => p.StockVenta)
            .Select(p => new ProductoStockBajoResumen
            {
                Descripcion = p.Descripcion,
                Stock = p.StockVenta,
                StockMinimo = umbral
            })
            .ToListAsync();

        var ultimasVentas = await context.Ventas
            .AsNoTracking()
            .Where(v => v.Estado)
            .OrderByDescending(v => v.Fecha)
            .Take(5)
            .Select(v => new UltimaVentaResumen
            {
                NumeroVenta = v.Id,
                ClienteNombre = v.Receta == null
                    ? "Consumidor final"
                    : v.Receta.Afiliado.Nombre + " " + v.Receta.Afiliado.Apellido,
                Total = v.Detalles.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario - d.Descuento)) ?? 0,
                Fecha = v.Fecha
            })
            .ToListAsync();

        

        return new DashboardResumen
        {
            VentasDelDia = totalHoy,
            VentasDelMes = totalMes,
            VariacionVentasDelDiaPorcentaje = Variacion(totalHoy, totalAyer),
            VariacionVentasDelMesPorcentaje = Variacion(totalMes, totalMesAnterior),
            RecetasAtendidasHoy = ventas.Count(v => v.ConReceta && v.Fecha >= hoy),
            ProductosActivos = await context.Productos.CountAsync(p => p.Estado),
            ProductosStockBajo = stockBajo.Count,
            VentasUltimos7Dias = puntosPorDia,
            ProductosConStockBajo = stockBajo.Take(6).ToList(),
            UltimasVentas = ultimasVentas,
            ProximosVencimientos = new List<VencimientoResumen>()   // la base no guarda vencimientos
        };
    }

    private static decimal Variacion(decimal actual, decimal anterior) =>
        anterior == 0 ? 0 : Math.Round((actual - anterior) / anterior * 100, 0);
}
