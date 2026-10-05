using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.ViewModels.Farmaceutico
{
	/// <summary>Inicio del Farmacéutico: indicadores y recetas pendientes, leídos de la base.</summary>
	public partial class DashboardFarmaceuticoViewModel : ObservableObject
	{
		private readonly RecetaService _recetaService;
		private readonly StockService _stockService;
		private readonly VentaService _ventaService;
		private readonly SesionUsuarioService _sesion;

		// ---- Tarjetas de resumen ----
		[ObservableProperty]
		private int recetasPendientes;

		[ObservableProperty]
		private int ventasHoy;

		[ObservableProperty]
		private int ventasConRecetaHoy;

		[ObservableProperty]
		private int stockCritico;

		[ObservableProperty]
		private int pendientesCobro;

		[ObservableProperty]
		private bool sinRecetasPendientes;

		public ObservableCollection<RecetaPendienteVm> RecetasPendientesList { get; } = new();

		

		public DashboardFarmaceuticoViewModel(RecetaService recetaService, StockService stockService,
			VentaService ventaService, SesionUsuarioService sesion)
		{
			_recetaService = recetaService;
			_stockService = stockService;
			_ventaService = ventaService;
			_sesion = sesion;
		}

		public async Task CargarDatosAsync()
		{
			var recetas = await _recetaService.ObtenerRecetasAsync();
			var pendientes = recetas.Where(r => r.Estado == Receta.Pendiente).OrderBy(r => r.FechaVencimiento).ToList();

			RecetasPendientesList.Clear();
			foreach (var r in pendientes.Take(8))
			{
				string medicamentos = string.Join(", ", r.Medicamentos.Select(m => m.Producto));
				RecetasPendientesList.Add(new RecetaPendienteVm(r.Paciente, medicamentos, r.Medico, r.FechaEmision));
			}

			RecetasPendientes = pendientes.Count;
			SinRecetasPendientes = pendientes.Count == 0;

			var stock = await _stockService.ObtenerStockAsync();
			StockCritico = stock.Count(p => p.Estado != EstadosStock.Normal);

			var usuarioId = _sesion.UsuarioActual?.Id;
			var misVentas = await _ventaService.ObtenerVentasAsync(usuarioId);
			var activasHoy = misVentas
				.Where(v => v.Fecha.Date == DateTime.Today && v.Estado != VentaService.EstadoAnulada)
				.ToList();
			VentasHoy = activasHoy.Count;
			VentasConRecetaHoy = activasHoy.Count(v => v.TieneReceta);
			PendientesCobro = misVentas.Count(v => v.Estado == VentaService.EstadoPendiente);
		}
	}

	public record RecetaPendienteVm(string Paciente, string Medicamento, string Medico, DateTime FechaEmision);
}
