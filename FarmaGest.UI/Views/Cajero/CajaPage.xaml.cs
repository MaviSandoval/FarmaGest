using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.Views.Cajero
{
    public partial class CajaPage : Page
    {
        private readonly CajaService _cajaService;
        private readonly SesionUsuarioService _sesionUsuarioService;

        public CajaPage(
            CajaService cajaService,
            SesionUsuarioService sesionUsuarioService)
        {
            InitializeComponent();

            _cajaService = cajaService;
            _sesionUsuarioService = sesionUsuarioService;

            Loaded += CajaPage_Loaded;
        }

        private async void CajaPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ActualizarEstadoCajaAsync();
        }

        private async Task ActualizarEstadoCajaAsync()
        {
            var usuario = _sesionUsuarioService.UsuarioActual;

            if (usuario == null)
            {
                MessageBox.Show(
                    "No hay un usuario autenticado.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var caja = await _cajaService.ObtenerCajaAbiertaAsync(usuario.Id);

            if (caja == null)
            {
                PanelCajaCerrada.Visibility = Visibility.Visible;
                PanelCajaAbierta.Visibility = Visibility.Collapsed;
                return;
            }

            PanelCajaCerrada.Visibility = Visibility.Collapsed;
            PanelCajaAbierta.Visibility = Visibility.Visible;

            var totalVentas = await _cajaService.ObtenerTotalVentasAsync(caja.Id);
            var montoEsperado = await _cajaService.ObtenerMontoEsperadoAsync(caja.Id);

            var cultura = new CultureInfo("es-AR");

            TxtFechaApertura.Text =
                $"Abierta desde: {caja.FechaApertura:dd/MM/yyyy HH:mm}";

            TxtMontoInicialActual.Text =
                caja.MontoInicial.ToString("C", cultura);

            TxtTotalVentas.Text =
                totalVentas.ToString("C", cultura);

            TxtMontoEsperado.Text =
                montoEsperado.ToString("C", cultura);
        }

        private async void AbrirCaja_Click(object sender, RoutedEventArgs e)
        {
            var usuario = _sesionUsuarioService.UsuarioActual;

            if (usuario == null)
                return;

            if (!decimal.TryParse(TxtMontoInicial.Text, out decimal montoInicial))
            {
                MessageBox.Show(
                    "Ingrese un monto inicial válido.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (montoInicial < 0)
            {
                MessageBox.Show(
                    "El monto inicial no puede ser negativo.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                await _cajaService.AbrirCajaAsync(usuario.Id, montoInicial);

                TxtMontoInicial.Clear();

                await ActualizarEstadoCajaAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void CerrarCaja_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "El cierre de caja se implementará en el siguiente paso.",
                "FarmaGest",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await Task.CompletedTask;
        }
    }
}