using FarmaGest.Datos.Contexto;
using FarmaGest.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FarmaGest.Negocio.Servicios;

public class DashboardServiceEnMemoria : IDashboardService
{
	private readonly IDbContextFactory<FarmaGestDbContext> _factory;
	private const int UmbralStockBajo = 10; // ajustar según el criterio real de la farmacia

	public DashboardServiceEnMemoria(IDbContextFactory<FarmaGestDbContext> factory)
	{
		_factory = factory;
	}

	public async Task<DashboardResumen> ObtenerResumenAsync()
	{
		await using var context = await _factory.CreateDbContextAsync();

		var hoy = DateTime.Today;
		var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
		var hace7Dias = hoy.AddDays(-6);

		var ventasHoy = await context.Ventas
			.Include(v => v.Detalles)
			.Where(v => v.Estado && v.Fecha.Date == hoy)
			.ToListAsync();

		var ventasMes = await context.Ventas
			.Include(v => v.Detalles)
			.Where(v => v.Estado && v.Fecha >= inicioMes)
			.ToListAsync();

		decimal totalHoy = ventasHoy.Sum(v => v.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento));
		decimal totalMes = ventasMes.Sum(v => v.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento));

		var productosStockBajo = await context.Productos
			.Where(p => p.Estado && p.StockVenta <= UmbralStockBajo)
			.ToListAsync();

		var ventasUltimos7Dias = await context.Ventas
			.Where(v => v.Estado && v.Fecha.Date >= hace7Dias)
			.Include(v => v.Detalles)
			.ToListAsync();

		var puntosPorDia = Enumerable.Range(0, 7)
			.Select(offset => hace7Dias.AddDays(offset))
			.Select(dia => new PuntoVentaDiaria
			{
				Dia = dia.ToString("ddd", new System.Globalization.CultureInfo("es-AR")),
				Total = ventasUltimos7Dias
					.Where(v => v.Fecha.Date == dia)
					.Sum(v => v.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario - d.Descuento))
			})
			.ToList();

		var ultimasVentas = await context.Ventas
			.Where(v => v.Estado)
			.OrderByDescending(v => v.Fecha)
			.Take(5)
			.ToListAsync();

		int recetasAtendidasHoy = await context.Ventas
			.CountAsync(v => v.Estado && v.RecetaId != null && v.Fecha.Date == hoy);

		return new DashboardResumen
		{
			VentasDelDia = totalHoy,
			VentasDelMes = totalMes,
			VariacionVentasDelDiaPorcentaje = 0,   // requiere comparar contra el día anterior; lo dejamos en 0 por ahora
			VariacionVentasDelMesPorcentaje = 0,   // ídem, contra el mes anterior
			RecetasAtendidasHoy = recetasAtendidasHoy,
			ProductosActivos = await context.Productos.CountAsync(p => p.Estado),
			ProductosStockBajo = productosStockBajo.Count,
			VentasUltimos7Dias = puntosPorDia,
			ProductosConStockBajo = productosStockBajo,
			UltimasVentas = ultimasVentas,
			ProximosVencimientos = new List<Producto>() // sin implementar por ahora, ver nota arriba
		};
	}
}