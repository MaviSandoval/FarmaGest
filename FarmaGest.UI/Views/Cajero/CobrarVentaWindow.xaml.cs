using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero
{
    public partial class CobrarVentaWindow : Window
    {
        public int VentaId { get; }

        public decimal Total { get; }

        public string MetodoPagoSeleccionado { get; private set; }
            = string.Empty;

        public CobrarVentaWindow(int ventaId, decimal total)
        {
            InitializeComponent();

            VentaId = ventaId;
            Total = total;

            var cultura = new CultureInfo("es-AR");

            TxtNumeroVenta.Text = $"Venta N° {VentaId}";

            TxtTotal.Text = Total.ToString("C", cultura);
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            if (CmbMetodoPago.SelectedItem is not ComboBoxItem item)
            {
                MessageBox.Show(
                    "Seleccione un método de pago.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MetodoPagoSeleccionado =
                item.Content?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(MetodoPagoSeleccionado))
            {
                MessageBox.Show(
                    "Seleccione un método de pago.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            DialogResult = true;
            Close();
        }
    }
}