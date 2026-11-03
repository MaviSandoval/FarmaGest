using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero
{
    public partial class CobrarVentaWindow : Window
    {
        private readonly CultureInfo _cultura =
            new("es-AR");

        private readonly FacturacionService _facturacionService;

        public int VentaId { get; }

        public decimal Total { get; }

        public string MetodoPagoSeleccionado { get; private set; }
            = string.Empty;

        public int MedioPagoIdSeleccionado { get; private set; }

        public decimal MontoCambio { get; private set; }

        // =====================================================
        // DATOS DE TARJETA QUE SE GUARDARÁN
        // =====================================================

        public string Ultimos4Tarjeta { get; private set; }
            = string.Empty;

        public string TitularTarjeta { get; private set; }
            = string.Empty;

        public string NumeroAutorizacion { get; private set; }
            = string.Empty;

        public int Cuotas { get; private set; } = 1;

        // =====================================================
        // DATOS DE TRANSFERENCIA
        // =====================================================

        public string NumeroOperacionTransferencia { get; private set; }
            = string.Empty;

        public CobrarVentaWindow(
            int ventaId,
            decimal total)
        {
            InitializeComponent();

            VentaId = ventaId;
            Total = total;

            _facturacionService =
                App.Services.GetRequiredService<FacturacionService>();

            TxtNumeroVenta.Text =
                $"Venta N° {VentaId}";

            TxtTotal.Text =
                Total.ToString("C", _cultura);

            TxtVuelto.Text =
                0m.ToString("C", _cultura);

            Loaded += CobrarVentaWindow_Loaded;
        }

        // =====================================================
        // CARGAR MEDIOS DE PAGO DESDE LA BASE DE DATOS
        // =====================================================

        private async void CobrarVentaWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var mediosPago =
                    await _facturacionService
                        .ObtenerMediosPagoActivosAsync();

                CmbMetodoPago.ItemsSource =
                    mediosPago;

                if (mediosPago.Count > 0)
                {
                    CmbMetodoPago.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudieron cargar los métodos de pago.\n\n" +
                    ex.Message,
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =====================================================
        // CAMBIO DE MÉTODO DE PAGO
        // =====================================================

        private void CmbMetodoPago_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (PanelEfectivo == null ||
                PanelTarjeta == null ||
                PanelTransferencia == null)
            {
                return;
            }

            // Ocultar todos los paneles
            PanelEfectivo.Visibility =
                Visibility.Collapsed;

            PanelTarjeta.Visibility =
                Visibility.Collapsed;

            PanelTransferencia.Visibility =
                Visibility.Collapsed;

            PanelCuotas.Visibility =
                Visibility.Collapsed;

            if (CmbMetodoPago.SelectedItem
                is not MedioPago medioPago)
            {
                return;
            }

            string metodo =
                medioPago.Nombre.Trim();

            // -------------------------------------------------
            // EFECTIVO
            // -------------------------------------------------

            if (metodo.Equals(
                    "Efectivo",
                    StringComparison.OrdinalIgnoreCase))
            {
                PanelEfectivo.Visibility =
                    Visibility.Visible;

                return;
            }

            // Al salir de efectivo reiniciamos el vuelto
            TxtEfectivoRecibido.Clear();

            MontoCambio = 0;

            TxtVuelto.Text =
                0m.ToString("C", _cultura);

            // -------------------------------------------------
            // DÉBITO
            // -------------------------------------------------

            if (metodo.Equals(
                    "Débito",
                    StringComparison.OrdinalIgnoreCase))
            {
                PanelTarjeta.Visibility =
                    Visibility.Visible;

                TxtTituloTarjeta.Text =
                    "Datos de tarjeta de débito";

                PanelCuotas.Visibility =
                    Visibility.Collapsed;

                return;
            }

            // -------------------------------------------------
            // CRÉDITO
            // -------------------------------------------------

            if (metodo.Equals(
                    "Crédito",
                    StringComparison.OrdinalIgnoreCase))
            {
                PanelTarjeta.Visibility =
                    Visibility.Visible;

                TxtTituloTarjeta.Text =
                    "Datos de tarjeta de crédito";

                PanelCuotas.Visibility =
                    Visibility.Visible;

                return;
            }

            // -------------------------------------------------
            // TRANSFERENCIA
            // -------------------------------------------------

            if (metodo.Equals(
                    "Transferencia",
                    StringComparison.OrdinalIgnoreCase))
            {
                PanelTransferencia.Visibility =
                    Visibility.Visible;
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
                is not MedioPago medioPago)
            {
                MessageBox.Show(
                    "Seleccione un método de pago.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MedioPagoIdSeleccionado =
                medioPago.Id;

            MetodoPagoSeleccionado =
                medioPago.Nombre;

            string metodo =
                medioPago.Nombre.Trim();

            // Limpiar valores anteriores
            MontoCambio = 0;
            Ultimos4Tarjeta = string.Empty;
            TitularTarjeta = string.Empty;
            NumeroAutorizacion = string.Empty;
            Cuotas = 1;
            NumeroOperacionTransferencia = string.Empty;

            // -------------------------------------------------
            // EFECTIVO
            // -------------------------------------------------

            if (metodo.Equals(
                    "Efectivo",
                    StringComparison.OrdinalIgnoreCase))
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

            // -------------------------------------------------
            // DÉBITO / CRÉDITO
            // -------------------------------------------------

            else if (
                metodo.Equals(
                    "Débito",
                    StringComparison.OrdinalIgnoreCase)
                ||
                metodo.Equals(
                    "Crédito",
                    StringComparison.OrdinalIgnoreCase))
            {
                string titular =
                    TxtTitularTarjeta.Text.Trim();

                // Permitimos que visualmente se escriba
                // con espacios o guiones.
                string numeroTarjeta =
                    new string(
                        TxtNumeroTarjeta.Text
                            .Where(char.IsDigit)
                            .ToArray());

                string vencimiento =
                    TxtVencimiento.Text.Trim();

                string cvv =
                    TxtCvv.Password.Trim();

                if (string.IsNullOrWhiteSpace(titular))
                {
                    MessageBox.Show(
                        "Ingrese el titular de la tarjeta.",
                        "FarmaGest",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (numeroTarjeta.Length < 13 ||
                    numeroTarjeta.Length > 19)
                {
                    MessageBox.Show(
                        "Ingrese un número de tarjeta válido.",
                        "FarmaGest",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (!EsVencimientoValido(vencimiento))
                {
                    MessageBox.Show(
                        "Ingrese un vencimiento válido en formato MM/AA.",
                        "FarmaGest",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if ((cvv.Length != 3 &&
                     cvv.Length != 4) ||
                    !cvv.All(char.IsDigit))
                {
                    MessageBox.Show(
                        "Ingrese un CVV válido.",
                        "FarmaGest",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                Ultimos4Tarjeta =
                    numeroTarjeta[^4..];

                TitularTarjeta =
                    titular;

                // Simulamos la autorización del procesador.
                NumeroAutorizacion =
                    Guid.NewGuid()
                        .ToString("N")
                        .Substring(0, 10)
                        .ToUpperInvariant();

                // Crédito permite cuotas
                if (metodo.Equals(
                        "Crédito",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (CmbCuotas.SelectedItem
                        is not ComboBoxItem cuotaSeleccionada ||
                        !int.TryParse(
                            cuotaSeleccionada.Content?.ToString(),
                            out int cantidadCuotas))
                    {
                        MessageBox.Show(
                            "Seleccione la cantidad de cuotas.",
                            "FarmaGest",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    Cuotas =
                        cantidadCuotas;
                }
                else
                {
                    // Débito siempre queda en una cuota
                    Cuotas = 1;
                }
            }

            // -------------------------------------------------
            // TRANSFERENCIA
            // -------------------------------------------------

            else if (metodo.Equals(
                    "Transferencia",
                    StringComparison.OrdinalIgnoreCase))
            {
                string numeroOperacion =
                    TxtNumeroOperacion.Text.Trim();

                if (string.IsNullOrWhiteSpace(
                        numeroOperacion))
                {
                    MessageBox.Show(
                        "Ingrese el número de operación o comprobante.",
                        "FarmaGest",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                NumeroOperacionTransferencia =
                    numeroOperacion;
            }

            DialogResult = true;
            Close();
        }

        // =====================================================
        // VALIDAR VENCIMIENTO MM/AA
        // =====================================================

        private static bool EsVencimientoValido(
            string vencimiento)
        {
            if (string.IsNullOrWhiteSpace(vencimiento))
                return false;

            string[] partes =
                vencimiento.Split('/');

            if (partes.Length != 2)
                return false;

            if (!int.TryParse(
                    partes[0],
                    out int mes))
            {
                return false;
            }

            if (!int.TryParse(
                    partes[1],
                    out int anio))
            {
                return false;
            }

            if (mes < 1 || mes > 12)
                return false;

            if (partes[1].Length != 2)
                return false;

            int anioCompleto =
                2000 + anio;

            DateTime ultimoDiaMes =
                new(
                    anioCompleto,
                    mes,
                    DateTime.DaysInMonth(
                        anioCompleto,
                        mes));

            return ultimoDiaMes.Date >=
                   DateTime.Today;
        }
    }
}