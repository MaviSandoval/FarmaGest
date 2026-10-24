using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Cajero
{
    public partial class CajaPage : Page
    {
        private readonly CajaService _cajaService;
        private readonly SesionUsuarioService _sesionUsuarioService;
        private readonly UsuarioService _usuarioService;

        public CajaPage(
            CajaService cajaService,
            SesionUsuarioService sesionUsuarioService,
            UsuarioService usuarioService)
        {
            InitializeComponent();

            _cajaService = cajaService;
            _sesionUsuarioService = sesionUsuarioService;
            _usuarioService = usuarioService;

            Loaded += CajaPage_Loaded;
        }

        // =====================================================
        // CARGAR PÁGINA
        // =====================================================

        private async void CajaPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await ActualizarEstadoCajaAsync();
        }

        // =====================================================
        // ACTUALIZAR ESTADO
        // =====================================================

        private async Task ActualizarEstadoCajaAsync()
        {
            var usuario =
                _sesionUsuarioService.UsuarioActual;

            if (usuario == null)
            {
                MessageBox.Show(
                    "No hay un usuario autenticado.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var caja =
                await _cajaService.ObtenerCajaAbiertaAsync(
                    usuario.Id);

            // CAJA CERRADA
            if (caja == null)
            {
                PanelCajaCerrada.Visibility =
                    Visibility.Visible;

                PanelCajaAbierta.Visibility =
                    Visibility.Collapsed;

                return;
            }

            // CAJA ABIERTA
            PanelCajaCerrada.Visibility =
                Visibility.Collapsed;

            PanelCajaAbierta.Visibility =
                Visibility.Visible;

            var totalVentas =
                await _cajaService.ObtenerTotalVentasAsync(
                    caja.Id);

            var montoEsperado =
                await _cajaService.ObtenerMontoEsperadoAsync(
                    caja.Id);

            var cultura =
                new CultureInfo("es-AR");

            TxtFechaApertura.Text =
                $"Abierta desde: " +
                $"{caja.FechaApertura:dd/MM/yyyy HH:mm}";

            TxtMontoInicialActual.Text =
                caja.MontoInicial.ToString(
                    "C",
                    cultura);

            TxtTotalVentas.Text =
                totalVentas.ToString(
                    "C",
                    cultura);

            TxtMontoEsperado.Text =
                montoEsperado.ToString(
                    "C",
                    cultura);
        }

        // =====================================================
        // ABRIR CAJA
        // =====================================================

        private async void AbrirCaja_Click(
            object sender,
            RoutedEventArgs e)
        {
            var usuario =
                _sesionUsuarioService.UsuarioActual;

            if (usuario == null)
            {
                MessageBox.Show(
                    "No hay un usuario autenticado.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // -------------------------------------------------
            // VALIDAR MONTO INICIAL
            // -------------------------------------------------

            var montoLeido =
                ValidacionEntrada.LeerDecimal(
                    TxtMontoInicial.Text);

            if (montoLeido == null)
            {
                MessageBox.Show(
                    "Ingrese un monto inicial válido.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            decimal montoInicial =
                montoLeido.Value;

            if (montoInicial < 0)
            {
                MessageBox.Show(
                    "El monto inicial no puede ser negativo.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // -------------------------------------------------
            // AUTORIZACIÓN DEL ADMINISTRADOR
            // -------------------------------------------------

            var ventanaAutorizacion =
                new AutorizacionAdministradorWindow(
                    _usuarioService)
                {
                    Owner =
                        Window.GetWindow(this)
                };

            var autorizado =
                ventanaAutorizacion.ShowDialog();

            if (autorizado != true)
                return;

            // -------------------------------------------------
            // ABRIR CAJA
            // -------------------------------------------------

            try
            {
                await _cajaService.AbrirCajaAsync(
                    usuario.Id,
                    montoInicial);

                TxtMontoInicial.Clear();

                await ActualizarEstadoCajaAsync();

                MessageBox.Show(
                    "Caja abierta correctamente.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo abrir la caja.\n\n" +
                    ex.Message,
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =====================================================
        // CERRAR CAJA
        // =====================================================

        private async void CerrarCaja_Click(
            object sender,
            RoutedEventArgs e)
        {
            var ventana =
                new CierreCajaWindow(
                    _cajaService,
                    _sesionUsuarioService)
                {
                    Owner =
                        Window.GetWindow(this)
                };

            ventana.ShowDialog();

            if (ventana.CajaCerrada)
            {
                await ActualizarEstadoCajaAsync();
            }
        }
    }
}