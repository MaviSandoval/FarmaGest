using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Helpers;
using Wpf.Ui.Controls;

using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace FarmaGest.UI.Views.Cajero;

public partial class CierreCajaWindow : FluentWindow
{
    private static readonly CultureInfo Cultura = new("es-AR");

    private readonly CajaService _cajaService;
    private readonly SesionUsuarioService _sesionUsuarioService;

    private decimal _efectivoEsperado;

    public bool CajaCerrada { get; private set; }

    public CierreCajaWindow(
        CajaService cajaService,
        SesionUsuarioService sesionUsuarioService)
    {
        InitializeComponent();

        _cajaService = cajaService;
        _sesionUsuarioService = sesionUsuarioService;

        Loaded += CierreCajaWindow_Loaded;
    }

    // =========================================================
    // CARGAR DATOS REALES DE LA CAJA
    // =========================================================

    private async void CierreCajaWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var usuario = _sesionUsuarioService.UsuarioActual;

            if (usuario == null)
            {
                MessageBox.Show(
                    "No hay un usuario autenticado.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Close();
                return;
            }

            var caja =
                await _cajaService.ObtenerCajaAbiertaAsync(usuario.Id);

            if (caja == null)
            {
                MessageBox.Show(
                    "No hay una caja abierta.",
                    "FarmaGest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Close();
                return;
            }

            var efectivo =
                await _cajaService.ObtenerTotalEfectivoAsync(caja.Id);

            var cantidadEfectivo =
                await _cajaService.ObtenerCantidadCobrosEfectivoAsync(caja.Id);

            var otrosMedios =
                await _cajaService.ObtenerTotalOtrosMediosAsync(caja.Id);

            var cantidadOtros =
                await _cajaService.ObtenerCantidadOtrosMediosAsync(caja.Id);

            _efectivoEsperado =
                await _cajaService.ObtenerMontoEsperadoAsync(caja.Id);

            // Estos nombres los agregaremos ahora al XAML.
            TxtInfoCaja.Text =
                $"Caja N° {caja.Id} · Abierta a las {caja.FechaApertura:HH:mm}. " +
                "Contá el efectivo del cajón e ingresalo abajo.";

            TxtMontoInicial.Text =
                caja.MontoInicial.ToString("C", Cultura);

            TxtCantidadEfectivo.Text =
                $"Cobros en efectivo ({cantidadEfectivo})";

            TxtTotalEfectivo.Text =
                efectivo.ToString("C", Cultura);

            TxtEfectivoEsperado.Text =
                _efectivoEsperado.ToString("C", Cultura);

            TxtCantidadOtros.Text =
                $"Débito · Crédito · Transferencia ({cantidadOtros})";

            TxtTotalOtros.Text =
                otrosMedios.ToString("C", Cultura);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo cargar la información de la caja.\n\n" +
                ex.Message,
                "FarmaGest",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Close();
        }
    }

    // =========================================================
    // DIFERENCIA
    // =========================================================

    private void EfectivoContado_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        var contado =
            ValidacionEntrada.LeerDecimal(EfectivoContadoText.Text);

        if (contado == null)
        {
            DiferenciaText.Text = "—";

            DiferenciaText.Foreground =
                (Brush)FindResource("FarmaTextMutedBrush");

            return;
        }

        var diferencia =
            contado.Value - _efectivoEsperado;

        DiferenciaText.Text =
            diferencia == 0
                ? "Sin diferencia"
                : (diferencia > 0 ? "Sobrante " : "Faltante ") +
                  Math.Abs(diferencia).ToString("C", Cultura);

        DiferenciaText.Foreground =
            (Brush)FindResource(
                diferencia == 0
                    ? "FarmaSuccessBrush"
                    : "FarmaDangerBrush");
    }

    // =========================================================
    // CONFIRMAR CIERRE
    // =========================================================

    private async void Confirmar_Click(
        object sender,
        RoutedEventArgs e)
    {
        MensajeText.Text = string.Empty;

        var contado =
            ValidacionEntrada.LeerDecimal(EfectivoContadoText.Text);

        if (contado == null)
        {
            MensajeText.Text =
                "Ingresá el efectivo contado.";
            return;
        }

        if (contado.Value < 0)
        {
            MensajeText.Text =
                "El efectivo contado no puede ser negativo.";
            return;
        }

        if (contado.Value != _efectivoEsperado &&
            string.IsNullOrWhiteSpace(ObservacionesText.Text))
        {
            MensajeText.Text =
                "Hay diferencia con lo esperado: escribí una observación.";
            return;
        }

        var usuario = _sesionUsuarioService.UsuarioActual;

        if (usuario == null)
            return;

        try
        {
            await _cajaService.CerrarCajaAsync(
                usuario.Id,
                contado.Value);

            CajaCerrada = true;

            MessageBox.Show(
                "Caja cerrada correctamente.",
                "FarmaGest",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MensajeText.Text =
                "No se pudo cerrar la caja: " + ex.Message;
        }
    }

    private void Cancelar_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}