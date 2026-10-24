using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using FarmaGest.Negocio.Servicios;

using Microsoft.Extensions.DependencyInjection;

namespace FarmaGest.UI.Views.Cajero;

public partial class DashboardCajeroPage : Page
{
    private readonly FacturacionService _facturacionService;
    private readonly SesionUsuarioService _sesionUsuarioService;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public DashboardCajeroPage(
        FacturacionService facturacionService,
        SesionUsuarioService sesionUsuarioService)
    {
        InitializeComponent();

        _facturacionService = facturacionService;
        _sesionUsuarioService = sesionUsuarioService;

        Loaded += DashboardCajeroPage_Loaded;
    }

    // =========================================================
    // CARGAR DASHBOARD
    // =========================================================

    private async void DashboardCajeroPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await CargarDashboardAsync();
    }

    private async Task CargarDashboardAsync()
    {
        try
        {
            var usuario =
                _sesionUsuarioService.UsuarioActual;

            if (usuario == null)
                return;

            // =================================================
            // VENTAS PENDIENTES DE FACTURACIÓN
            // =================================================

            var preventas =
                await _facturacionService
                    .ObtenerVentasPendientesAsync();

            TxtPreventasEspera.Text =
                preventas.Count.ToString();

            PreventasGrid.ItemsSource =
                preventas
                    .Select(v => new PreventaVista
                    {
                        Numero = v.VentaId,

                        Hora =
                            v.Fecha.ToString("HH:mm"),

                        Responsable =
                            v.Responsable,

                        Items =
                            v.CantidadProductos,

                        Total =
                            v.Total
                    })
                    .ToList();

            // =================================================
            // RESUMEN DE CAJA
            // =================================================

            var resumen =
                await _facturacionService
                    .ObtenerResumenCajaAbiertaAsync(
                        usuario.Id);

            // =================================================
            // NO HAY CAJA ABIERTA
            // =================================================

            if (resumen == null)
            {
                TxtEstadoCaja.Text =
                    "No hay una caja abierta";

                TxtDetalleCaja.Text =
                    "Abrí una caja para comenzar a cobrar.";

                TxtCobrosHoy.Text =
                    "0";

                TxtTotalCobrado.Text =
                    "$ 0,00";

                TxtEfectivoCaja.Text =
                    "$ 0,00";

                MediosPagoList.ItemsSource =
                    null;

                return;
            }

            // =================================================
            // INFORMACIÓN DE LA CAJA
            // =================================================

            TxtEstadoCaja.Text =
                $"Caja N° {resumen.CajaId} · Abierta";

            TxtDetalleCaja.Text =
                $"Desde las {resumen.FechaApertura:HH:mm} · " +
                $"Monto inicial {resumen.MontoInicial:$ #,##0.00}";

            // =================================================
            // KPIs
            // =================================================

            TxtCobrosHoy.Text =
                resumen.CantidadCobros.ToString();

            TxtTotalCobrado.Text =
                resumen.TotalCobrado.ToString(
                    "$ #,##0.00");

            TxtEfectivoCaja.Text =
                resumen.EfectivoEsperado.ToString(
                    "$ #,##0.00");

            // =================================================
            // MEDIOS DE PAGO
            // =================================================

            const double anchoMaximo = 260;

            decimal mayorImporte =
                resumen.MediosPago.Count > 0
                    ? resumen.MediosPago.Max(m => m.Importe)
                    : 0;

            MediosPagoList.ItemsSource =
                resumen.MediosPago
                    .Select(m => new MedioPagoVista
                    {
                        Medio =
                            m.Medio,

                        Cantidad =
                            m.Cantidad,

                        Importe =
                            m.Importe,

                        AnchoBarra =
                            mayorImporte > 0
                                ? (double)
                                    (m.Importe / mayorImporte)
                                    * anchoMaximo
                                : 0
                    })
                    .ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo cargar el inicio del cajero.\n\n" +
                ex.Message,
                "FarmaGest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // =========================================================
    // VER RESUMEN
    // =========================================================

    private void VerResumen_Click(
        object sender,
        RoutedEventArgs e)
    {
        var pagina =
            App.Services
                .GetRequiredService<ResumenCajaPage>();

        NavigationService?.Navigate(
            pagina);
    }

    // =========================================================
    // MODELO PARA PREVENTAS
    // =========================================================

    private class PreventaVista
    {
        public int Numero { get; set; }

        public string Hora { get; set; } =
            string.Empty;

        public string Responsable { get; set; } =
            string.Empty;

        public int Items { get; set; }

        public decimal Total { get; set; }

        public string Estado =>
            "Pendiente de cobro";
    }

    // =========================================================
    // MODELO PARA MEDIOS DE PAGO
    // =========================================================

    private class MedioPagoVista
    {
        public string Medio { get; set; } =
            string.Empty;

        public int Cantidad { get; set; }

        public decimal Importe { get; set; }

        public double AnchoBarra { get; set; }
    }
}