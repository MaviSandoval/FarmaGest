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
    private readonly UsuarioService _usuarioService;

    private decimal _efectivoEsperado;
    private int _cajaId;
    private bool _confirmando;

    public bool CajaCerrada { get; private set; }

    public CierreCajaWindow(
        CajaService cajaService,
        SesionUsuarioService sesionUsuarioService,
        UsuarioService usuarioService)
    {
        InitializeComponent();

        _cajaService = cajaService;
        _sesionUsuarioService = sesionUsuarioService;
        _usuarioService = usuarioService;

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
                await _cajaService.ObtenerCajaAbiertaAsync(
                    usuario.Id);

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

            _cajaId = caja.Id;
            await ActualizarTotalesAsync();

            TxtInfoCaja.Text =
                $"Caja N° {caja.Id} · Abierta a las " +
                $"{caja.FechaApertura:HH:mm}. " +
                "Contá el efectivo del cajón e ingresalo abajo.";

            TxtMontoInicial.Text =
                caja.MontoInicial.ToString(
                    "C",
                    Cultura);

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

    private async System.Threading.Tasks.Task ActualizarTotalesAsync()
    {
        var totales = await _cajaService.ObtenerTotalesAsync(_cajaId);
        _efectivoEsperado = totales.EfectivoEsperado;
        TxtCantidadEfectivo.Text = $"Cobros en efectivo ({totales.CantidadEfectivo})";
        TxtTotalEfectivo.Text = totales.TotalEfectivo.ToString("C", Cultura);
        TxtEfectivoEsperado.Text = _efectivoEsperado.ToString("C", Cultura);
        TxtCantidadOtros.Text = $"Débito · Crédito · Transferencia ({totales.CantidadCobros - totales.CantidadEfectivo})";
        TxtTotalOtros.Text = totales.TotalOtrosMedios.ToString("C", Cultura);
        ActualizarDiferencia();
    }
    private void EfectivoContado_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ActualizarDiferencia();
    }

    private void ActualizarDiferencia()
    {
        var contado =
            ValidacionEntrada.LeerDecimal(
                EfectivoContadoText.Text);

        if (contado == null)
        {
            DiferenciaText.Text = "—";

            DiferenciaText.Foreground =
                (Brush)FindResource(
                    "FarmaTextMutedBrush");

            return;
        }

        var diferencia =
            contado.Value - _efectivoEsperado;

        DiferenciaText.Text =
            diferencia == 0
                ? "Sin diferencia"
                : (diferencia > 0
                    ? "Sobrante "
                    : "Faltante ") +
                  Math.Abs(diferencia)
                      .ToString("C", Cultura);

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
        if (_confirmando) return;
        MensajeText.Text = string.Empty;

        // -----------------------------------------------------
        // VALIDAR EFECTIVO CONTADO
        // -----------------------------------------------------

        var contado =
            ValidacionEntrada.LeerDecimal(
                EfectivoContadoText.Text);

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

        var usuario =
            _sesionUsuarioService.UsuarioActual;

        if (usuario == null)
        {
            MensajeText.Text =
                "No hay un usuario autenticado.";

            return;
        }

        // Validar contra los cobros actuales antes de solicitar autorización.
        try
        {
            _confirmando = true;
            var cajaActual = await _cajaService.ObtenerCajaAbiertaAsync(usuario.Id);
            if (cajaActual == null || cajaActual.Id != _cajaId)
            {
                MensajeText.Text = "La caja cambió o ya fue cerrada. Volvé a abrir el arqueo.";
                return;
            }
            await ActualizarTotalesAsync();
            if (contado.Value != _efectivoEsperado && string.IsNullOrWhiteSpace(ObservacionesText.Text))
            {
                MensajeText.Text = "Hay diferencia con lo esperado: escribí una observación.";
                return;
            }
        }
        catch (Exception ex)
        {
            MensajeText.Text = "No se pudo actualizar el arqueo: " + ex.Message;
            return;
        }
        finally { _confirmando = false; }
        // -----------------------------------------------------
        // AUTORIZACIÓN DEL FARMACÉUTICO
        // -----------------------------------------------------

        var ventanaAutorizacion =
            new AutorizacionFarmaceuticoWindow(
                _usuarioService)
            {
                Owner = this
            };

        var autorizado =
            ventanaAutorizacion.ShowDialog();

        if (autorizado != true)
        {
            MensajeText.Text =
                "El cierre de caja no fue autorizado.";

            return;
        }

        // -----------------------------------------------------
        // CERRAR CAJA
        // -----------------------------------------------------

        try
        {
            _confirmando = true;
            var cajaActual = await _cajaService.ObtenerCajaAbiertaAsync(usuario.Id);
            if (cajaActual == null || cajaActual.Id != _cajaId)
            {
                MensajeText.Text = "La caja cambió o ya fue cerrada. Volvé a abrir el arqueo.";
                return;
            }
            var esperadoAnterior = _efectivoEsperado;
            await ActualizarTotalesAsync();
            if (esperadoAnterior != _efectivoEsperado)
            {
                MensajeText.Text = "Los cobros cambiaron. Revisá el efectivo actualizado y confirmá nuevamente.";
                return;
            }
            if (contado.Value != _efectivoEsperado && string.IsNullOrWhiteSpace(ObservacionesText.Text))
            {
                MensajeText.Text = "Hay diferencia con lo esperado: escribí una observación.";
                return;
            }
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
                "No se pudo cerrar la caja: " +
                ex.Message;
        }
        finally { _confirmando = false; }
    }

    // =========================================================
    // CANCELAR
    // =========================================================

    private void Cancelar_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}