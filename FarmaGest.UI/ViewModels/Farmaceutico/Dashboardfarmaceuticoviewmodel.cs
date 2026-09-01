using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FarmaGest.UI.ViewModels.Farmaceutico
{
	public partial class DashboardFarmaceuticoViewModel : ObservableObject
	{
		// ---- Tarjetas de resumen ----
		[ObservableProperty]
		private int recetasPendientes;

		[ObservableProperty]
		private int consultasHoy;

		[ObservableProperty]
		private int ventasConRecetaHoy;

		[ObservableProperty]
		private int stockCritico;

		[ObservableProperty]
		private int vencimientosProximos;

		public ObservableCollection<RecetaPendienteVm> RecetasPendientesList { get; } = new();

		public ObservableCollection<AlertaVm> Alertas { get; } = new();

		public ObservableCollection<VencimientoVm> Vencimientos { get; } = new();

		public DashboardFarmaceuticoViewModel()
		{
			CargarDatos();
		}

		// TODO: reemplazar por llamadas reales a RecetaService / ProductoService / VentaService
		// (vía IDbContextFactory<FarmaGestDbContext>) apenas estén disponibles para este rol.
		private void CargarDatos()
		{
			RecetasPendientesList.Clear();
			RecetasPendientesList.Add(new RecetaPendienteVm("Juan Pérez", "Amoxicilina 500 mg x 16", "Dr. Alfredo Gómez", DateTime.Today));
			RecetasPendientesList.Add(new RecetaPendienteVm("María López", "Losartán 50 mg x 30", "Dra. Carla Ruiz", DateTime.Today.AddDays(-1)));
			RecetasPendientesList.Add(new RecetaPendienteVm("Consumidor Final", "Salbutamol 100 mcg x 200", "Dr. Nicolás Paz", DateTime.Today));

			Alertas.Clear();
			Alertas.Add(new AlertaVm("7 productos con stock crítico", "Revisar stock urgente", AlertaTipo.Critica));
			Alertas.Add(new AlertaVm("3 recetas pendientes de validar", "Ver detalles", AlertaTipo.Advertencia));
			Alertas.Add(new AlertaVm("12 productos por vencer en 30 días", "Revisar y gestionar", AlertaTipo.Advertencia));

			Vencimientos.Clear();
			Vencimientos.Add(new VencimientoVm("Loratadina 10 mg (x10)", new DateTime(2026, 9, 20), 15));
			Vencimientos.Add(new VencimientoVm("Diclofenac 50 mg (x20)", new DateTime(2026, 9, 22), 8));
			Vencimientos.Add(new VencimientoVm("Salbutamol 100 mcg (x200)", new DateTime(2026, 9, 28), 6));

			RecetasPendientes = RecetasPendientesList.Count;
			ConsultasHoy = 24;
			VentasConRecetaHoy = 9;
			StockCritico = 7;
			VencimientosProximos = Vencimientos.Count;
		}

		[RelayCommand]
		private void ValidarReceta(RecetaPendienteVm? receta)
		{
			if (receta is null)
			{
				return;
			}

			// TODO: abrir la vista de validación de receta / llamar a RecetaService.Validar(...)
			RecetasPendientesList.Remove(receta);
			RecetasPendientes = RecetasPendientesList.Count;
		}

		[RelayCommand]
		private void VerTodasLasRecetas()
		{
			// TODO: navegar a la vista completa de Recetas del Farmacéutico
		}

		[RelayCommand]
		private void VerTodosLosVencimientos()
		{
			// TODO: navegar a la vista completa de Stock / Vencimientos
		}
	}

	public record RecetaPendienteVm(string Paciente, string Medicamento, string Medico, DateTime FechaEmision);

	public record VencimientoVm(string Producto, DateTime FechaVencimiento, int Stock);

	public enum AlertaTipo
	{
		Critica,
		Advertencia,
		Informativa
	}

	public record AlertaVm(string Titulo, string Subtitulo, AlertaTipo Tipo);
}