using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero
{
    public partial class CobrarVentaWindow : Window
    {
        private readonly CultureInfo _cultura =
            new("es-AR");

        public int VentaId { get; }

        public decimal Total { get; }

        public string MetodoPagoSeleccionado { get; private set; }
            = string.Empty;

        public decimal MontoCambio { get; private set; }

        public CobrarVentaWindow(
            int ventaId,
            decimal total)
        {
            InitializeComponent();

            VentaId = ventaId;
            Total = total;

            TxtNumeroVenta.Text =
                $"Venta N° {VentaId}";

            TxtTotal.Text =
                Total.ToString("C", _cultura);

            TxtVuelto.Text =
                0m.ToString("C", _cultura);
        }

        // =====================================================
        // CAMBIO DE MÉTODO DE PAGO
        // =====================================================

        private void CmbMetodoPago_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (PanelEfectivo == null)
                return;

            var metodo =
                (CmbMetodoPago.SelectedItem as ComboBoxItem)
                ?.Content
                ?.ToString();

            bool esEfectivo =
                metodo == "Efectivo";

            PanelEfectivo.Visibility =
                esEfectivo
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (!esEfectivo)
            {
                TxtEfectivoRecibido.Clear();

                MontoCambio = 0;

                TxtVuelto.Text =
                    0m.ToString("C", _cultura);
            }
        }

        // =====================================================
        // CALCULAR VUELTO
        // =====================================================

        private void TxtEfectivoRecibido_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (TxtVuelto == null)
                return;

            if (!decimal.TryParse(
                    TxtEfectivoRecibido.Text,
                    NumberStyles.Number,
                    _cultura,
                    out decimal recibido))
            {
                MontoCambio = 0;

                TxtVuelto.Text =
                    0m.ToString("C", _cultura);

                return;
            }

            if (recibido < Total)
            {
                MontoCambio = 0;

                TxtVuelto.Text =
                    0m.ToString("C", _cultura);

                return;
            }

            MontoCambio =
                recibido - Total;

            TxtVuelto.Text =
                MontoCambio.ToString(
                    "C",
                    _cultura);
        }

        // =====================================================
        // CANCELAR
        // =====================================================

        private void Cancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // =====================================================
        // CONFIRMAR
        // =====================================================

        private void Confirmar_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (CmbMetodoPago.SelectedItem
                is not ComboBoxItem item)
            {
                MessageBox.Show(
                    "Seleccione un método de pago.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MetodoPagoSeleccionado =
                item.Content?.ToString()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    MetodoPagoSeleccionado))
            {
                MessageBox.Show(
                    "Seleccione un método de pago.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // -------------------------------------------------
            // VALIDAR EFECTIVO
            // -------------------------------------------------

            if (MetodoPagoSeleccionado == "Efectivo")
            {
                if (!decimal.TryParse(
                        TxtEfectivoRecibido.Text,
                        NumberStyles.Number,
                        _cultura,
                        out decimal recibido))
                {
                    MessageBox.Show(
                        "Ingrese el efectivo recibido.",
                        "FarmaGest",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (recibido < Total)
                {
                    MessageBox.Show(
                        "El efectivo recibido no alcanza " +
                        "para cubrir el total de la venta.",
                        "FarmaGest",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                MontoCambio =
                    recibido - Total;
            }
            else
            {
                MontoCambio = 0;
            }

            DialogResult = true;
            Close();
        }
    }
}