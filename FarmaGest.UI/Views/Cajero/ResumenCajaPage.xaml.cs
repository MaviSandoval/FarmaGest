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
    private readonly CajaService _cajaService;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ResumenCajaPage(
        FacturacionService facturacionService,
        SesionUsuarioService sesionUsuarioService,
        CajaService cajaService)
    {
        InitializeComponent();

        _facturacionService = facturacionService;
        _sesionUsuarioService = sesionUsuarioService;
        _cajaService = cajaService;

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

            var resumen =
                await _facturacionService
                    .ObtenerResumenCajaAbiertaAsync(
                        usuario.Id);

            // -------------------------------------------------
            // NO HAY CAJA ABIERTA
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
            // ENCABEZADO
            // -------------------------------------------------

            TxtInfoCaja.Text =
                $"Caja N° {resumen.CajaId} · " +
                $"Abierta hoy a las " +
                $"{resumen.FechaApertura:HH:mm} · " +
                $"{resumen.Cajero}";

            // -------------------------------------------------
            // KPIs
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
            // TOTALES POR MEDIO DE PAGO
            // -------------------------------------------------

            MediosPagoList.ItemsSource =
                resumen.MediosPago;

            TxtTotalMediosPago.Text =
                resumen.TotalCobrado.ToString(
                    "$ #,##0.00");

            // -------------------------------------------------
            // MOVIMIENTOS / COBROS
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

        // Si se confirmó el cierre real,
        // actualizamos el resumen.
        if (ventana.CajaCerrada)
        {
            await CargarResumenCajaAsync();
        }
    }

    // =========================================================
    // VER TICKET
    // =========================================================

    private void VerTicket_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is FrameworkElement elemento &&
            elemento.DataContext is MovimientoVista movimiento)
        {
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