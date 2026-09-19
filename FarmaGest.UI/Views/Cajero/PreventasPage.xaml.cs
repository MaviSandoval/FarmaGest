using FarmaGest.Negocio.Servicios;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero
{
    public partial class PreventasPage : Page
    {
        private readonly FacturacionService _facturacionService;
        private readonly SesionUsuarioService _sesionUsuarioService;

        public PreventasPage(
            FacturacionService facturacionService,
            SesionUsuarioService sesionUsuarioService)
        {
            InitializeComponent();

            _facturacionService = facturacionService;
            _sesionUsuarioService = sesionUsuarioService;

            Loaded += PreventasPage_Loaded;
        }

        private async void PreventasPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await CargarVentasPendientesAsync();
        }

        private async Task CargarVentasPendientesAsync()
        {
            try
            {
                var ventas =
                    await _facturacionService.ObtenerVentasPendientesAsync();

                DgPreventas.ItemsSource = ventas;

                TxtCantidadPreventas.Text =
                    $"{ventas.Count} venta(s) pendiente(s) de facturación";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudieron cargar las ventas pendientes.\n\n{ex.Message}",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void Cobrar_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                // Obtener la venta correspondiente al botón presionado
                if (sender is not Button boton ||
                    boton.DataContext is not VentaPendienteFacturacionDto venta)
                {
                    return;
                }

                // Verificar que exista un usuario con sesión iniciada
                var usuario = _sesionUsuarioService.UsuarioActual;

                if (usuario == null)
                {
                    MessageBox.Show(
                        "No hay un usuario con sesión iniciada.",
                        "FarmaGest",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Abrir ventana de cobro
                var ventanaCobro =
                    new CobrarVentaWindow(
                        venta.VentaId,
                        venta.Total);

                ventanaCobro.Owner =
                    Window.GetWindow(this);

                var resultado =
                    ventanaCobro.ShowDialog();

                // El usuario canceló
                if (resultado != true)
                    return;

                // Registrar la facturación
                await _facturacionService.FacturarVentaAsync(
                    venta.VentaId,
                    usuario.Id,
                    ventanaCobro.MetodoPagoSeleccionado);

                MessageBox.Show(
                    $"Venta N° {venta.VentaId} facturada correctamente.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Volver a cargar las ventas pendientes
                await CargarVentasPendientesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo realizar el cobro.\n\n{ex.Message}",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}