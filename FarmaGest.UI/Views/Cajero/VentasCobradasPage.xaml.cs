using FarmaGest.Negocio.Servicios;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero
{
    public partial class VentasCobradasPage : Page
    {
        private readonly FacturacionService _facturacionService;

        public VentasCobradasPage(
            FacturacionService facturacionService)
        {
            InitializeComponent();

            _facturacionService = facturacionService;

            Loaded += VentasCobradasPage_Loaded;
        }

        private async void VentasCobradasPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await CargarVentasCobradasAsync();
        }

        private async Task CargarVentasCobradasAsync()
        {
            try
            {
                var ventas =
                    await _facturacionService.ObtenerVentasCobradasAsync();

                DgVentasCobradas.ItemsSource = ventas;

                TxtCantidadVentas.Text =
                    $"Ventas cobradas: {ventas.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudieron cargar las ventas cobradas.\n\n{ex.Message}",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // MAQUETA: por ahora muestra un ticket de ejemplo
        private void VerTicket_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new TicketWindow { Owner = Window.GetWindow(this) };
            ventana.ShowDialog();
        }
    }
}