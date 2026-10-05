using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.Views.Cajero;

public partial class ResumenCajaPage : Page
{
    private readonly FacturacionService _facturacionService;
    private readonly SesionUsuarioService _sesionUsuarioService;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ResumenCajaPage(
        FacturacionService facturacionService,
        SesionUsuarioService sesionUsuarioService)
    {
        InitializeComponent();

        _facturacionService = facturacionService;
        _sesionUsuarioService = sesionUsuarioService;

        Loaded += ResumenCajaPage_Loaded;
    }

    // =========================================================
    // AL CARGAR LA PÁGINA
    // =========================================================

    private async void ResumenCajaPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await CargarResumenCajaAsync();
    }

    // =========================================================
    // CARGAR RESUMEN DE CAJA
    // =========================================================

    private async Task CargarResumenCajaAsync()
    {
        try
        {
            // -------------------------------------------------
            // 1. Obtener usuario logueado
            // -------------------------------------------------

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
            // 2. Obtener resumen real de la caja abierta
            // -------------------------------------------------

            var resumen =
                await _facturacionService
                    .ObtenerResumenCajaAbiertaAsync(
                        usuario.Id);

            // -------------------------------------------------
            // 3. Verificar que exista una caja abierta
            // -------------------------------------------------

            if (resumen == null)
            {
                TxtInfoCaja.Text =
                    "No hay una caja abierta.";

                TxtMontoInicial.Text =
                    "$ 0,00";

                TxtTotalCobrado.Text =
                    "$ 0,00";

                TxtEfectivoEsperado.Text =
                    "$ 0,00";

                TxtCantidadCobros.Text =
                    "0";

                TxtTotalMediosPago.Text =
                    "$ 0,00";

                MediosPagoList.ItemsSource =
                    null;

                MovimientosGrid.ItemsSource =
                    null;

                return;
            }

            // -------------------------------------------------
            // 4. Encabezado
            // -------------------------------------------------

            TxtInfoCaja.Text =
                $"Caja N° {resumen.CajaId} · " +
                $"Abierta hoy a las " +
                $"{resumen.FechaApertura:HH:mm} · " +
                $"{resumen.Cajero}";

            // -------------------------------------------------
            // 5. KPIs
            // -------------------------------------------------

            TxtMontoInicial.Text =
                resumen.MontoInicial.ToString(
                    "$ #,##0.00");

            TxtTotalCobrado.Text =
                resumen.TotalCobrado.ToString(
                    "$ #,##0.00");

            TxtEfectivoEsperado.Text =
                resumen.EfectivoEsperado.ToString(
                    "$ #,##0.00");

            TxtCantidadCobros.Text =
                resumen.CantidadCobros.ToString();

            // -------------------------------------------------
            // 6. TOTALES POR MEDIO DE PAGO
            // -------------------------------------------------

            MediosPagoList.ItemsSource =
                resumen.MediosPago;

            TxtTotalMediosPago.Text =
                resumen.TotalCobrado.ToString(
                    "$ #,##0.00");

            // -------------------------------------------------
            // 7. MOVIMIENTOS / COBROS
            // -------------------------------------------------

            MovimientosGrid.ItemsSource =
                resumen.Movimientos
                    .Select(m => new MovimientoVista
                    {
                        FacturacionId =
                            m.FacturacionId,

                        Hora =
                            m.Fecha.ToString("HH:mm"),

                        Numero =
                            m.VentaId,

                        // Farmacéutico/vendedor que
                        // realizó la venta.
                        Responsable =
                            m.Responsable,

                        Medio =
                            m.MetodoPago,

                        Importe =
                            m.Importe
                    })
                    .ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo cargar el resumen de caja.\n\n" +
                ex.Message,
                "FarmaGest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // =========================================================
    // CERRAR CAJA
    // =========================================================

    private void CerrarCaja_Click(
        object sender,
        RoutedEventArgs e)
    {
        var ventana =
            new CierreCajaWindow
            {
                Owner =
                    Window.GetWindow(this)
            };

        ventana.ShowDialog();
    }

    // =========================================================
    // VER TICKET
    // =========================================================

    private void VerTicket_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Obtenemos la fila seleccionada.
        if (sender is FrameworkElement elemento &&
            elemento.DataContext is MovimientoVista movimiento)
        {
            // Por ahora mantenemos la ventana actual
            // de ticket.
            //
            // Más adelante podemos pasarle
            // movimiento.FacturacionId o movimiento.Numero
            // para mostrar el ticket exacto.

            var ventana =
                new TicketWindow
                {
                    Owner =
                        Window.GetWindow(this)
                };

            ventana.ShowDialog();

            return;
        }

        var ticket =
            new TicketWindow
            {
                Owner =
                    Window.GetWindow(this)
            };

        ticket.ShowDialog();
    }

    // =========================================================
    // IMPRIMIR
    // =========================================================

    private void Imprimir_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBox.Show(
            "La impresión del resumen de caja todavía no está implementada.",
            "FarmaGest",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // =========================================================
    // MODELO UTILIZADO POR EL DATAGRID
    // =========================================================

    private class MovimientoVista
    {
        public int FacturacionId { get; set; }

        public string Hora { get; set; } =
            string.Empty;

        public int Numero { get; set; }

        public string Responsable { get; set; } =
            string.Empty;

        public string Medio { get; set; } =
            string.Empty;

        public decimal Importe { get; set; }
    }
}