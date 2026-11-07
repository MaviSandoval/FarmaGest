using FarmaGest.Negocio.Servicios;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero;

public partial class ResumenCajaPage : Page
{
    private static readonly System.Globalization.CultureInfo Cultura = new("es-AR");
    private readonly FacturacionService _facturacionService;
    private readonly SesionUsuarioService _sesionUsuarioService;
    private readonly CajaService _cajaService;
    private readonly UsuarioService _usuarioService;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public ResumenCajaPage(
        FacturacionService facturacionService,
        SesionUsuarioService sesionUsuarioService,
        CajaService cajaService,
        UsuarioService usuarioService)
    {
        InitializeComponent();

        _facturacionService = facturacionService;
        _sesionUsuarioService = sesionUsuarioService;
        _cajaService = cajaService;
        _usuarioService = usuarioService;

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
                    .ObtenerResumenCajaAsync(
                        usuario.Id);

            // -------------------------------------------------
            // NO HAY CAJA ABIERTA
            // -------------------------------------------------

            if (resumen == null)
            {
                TxtEstadoCaja.Text = "Sin cajas";
                PanelArqueo.Visibility = Visibility.Collapsed;
                TxtTotalEfectivo.Text = TxtOtrosMedios.Text = TxtTicketPromedio.Text = "$ 0,00";
                TxtVentaMayor.Text = "Sin cobros";
                TxtInfoCaja.Text =
                    "Todavía no tenés cajas registradas.";

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

            TxtEstadoCaja.Text = resumen.Estado ? "Caja abierta" : "Caja cerrada · último cierre";
            TxtEstadoCaja.Foreground = (System.Windows.Media.Brush)FindResource(
                resumen.Estado ? "FarmaSuccessBrush" : "FarmaTextMutedBrush");
            TxtInfoCaja.Text = $"Caja N° {resumen.CajaId} · {resumen.Cajero}\n" +
                $"Apertura: {resumen.FechaApertura:dd/MM/yyyy HH:mm}" +
                (resumen.Estado ? "" : $" · Cierre: {resumen.FechaCierre:dd/MM/yyyy HH:mm}");
            PanelArqueo.Visibility = resumen.Estado ? Visibility.Collapsed : Visibility.Visible;
            TxtEfectivoContado.Text = resumen.MontoFinal?.ToString("C", Cultura) ?? "No registrado";
            TxtDiferencia.Text = resumen.Diferencia is not decimal diferencia ? "No registrada" :
                diferencia == 0 ? "Sin diferencia" :
                $"{(diferencia > 0 ? "Sobrante" : "Faltante")} {Math.Abs(diferencia).ToString("C", Cultura)}";
            TxtDiferencia.Foreground = (System.Windows.Media.Brush)FindResource(
                resumen.Diferencia == 0 ? "FarmaSuccessBrush" : "FarmaDangerBrush");
            TxtTotalEfectivo.Text = resumen.TotalEfectivo.ToString("C", Cultura);
            TxtOtrosMedios.Text = resumen.TotalOtrosMedios.ToString("C", Cultura);
            TxtTicketPromedio.Text = resumen.TicketPromedio.ToString("C", Cultura);
            TxtVentaMayor.Text = resumen.VentaMayorImporte?.ToString("C", Cultura) ?? "Sin cobros";
            // -------------------------------------------------
            // KPIs
            // -------------------------------------------------

            TxtMontoInicial.Text =
                resumen.MontoInicial.ToString(
                    "C", Cultura);

            TxtTotalCobrado.Text =
                resumen.TotalCobrado.ToString(
                    "C", Cultura);

            TxtEfectivoEsperado.Text =
                resumen.EfectivoEsperado.ToString(
                    "C", Cultura);

            TxtCantidadCobros.Text =
                resumen.CantidadCobros.ToString();

            // -------------------------------------------------
            // TOTALES POR MEDIO DE PAGO
            // -------------------------------------------------

            MediosPagoList.ItemsSource =
                resumen.MediosPago;

            TxtTotalMediosPago.Text =
                resumen.TotalCobrado.ToString(
                    "C", Cultura);

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
                            m.Fecha.ToString("dd/MM HH:mm"),

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
                _sesionUsuarioService,
                _usuarioService)
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

    private async void VerTicket_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is FrameworkElement
            {
                DataContext: MovimientoVista movimiento
            })
        {
            await TicketWindow.AbrirAsync(
                _facturacionService,
                movimiento.Numero,
                Window.GetWindow(this));
        }
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