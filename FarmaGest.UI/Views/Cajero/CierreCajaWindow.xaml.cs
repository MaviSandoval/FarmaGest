using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FarmaGest.UI.Helpers;
using Wpf.Ui.Controls;

namespace FarmaGest.UI.Views.Cajero;

// MAQUETA: arqueo y cierre de caja. No guarda nada todavía (el cierre real usa CajaService.CerrarCajaAsync).
public partial class CierreCajaWindow : FluentWindow
{
    private static readonly CultureInfo Cultura = new("es-AR");
    private const decimal EfectivoEsperado = 104320.00m;

    public CierreCajaWindow()
    {
        InitializeComponent();
    }

    // Muestra la diferencia entre lo contado y lo esperado mientras se escribe
    private void EfectivoContado_TextChanged(object sender, TextChangedEventArgs e)
    {
        var contado = ValidacionEntrada.LeerDecimal(EfectivoContadoText.Text);

        if (contado == null)
        {
            DiferenciaText.Text = "—";
            DiferenciaText.Foreground = (Brush)FindResource("FarmaTextMutedBrush");
            return;
        }

        var diferencia = contado.Value - EfectivoEsperado;

        DiferenciaText.Text = diferencia == 0
            ? "Sin diferencia"
            : (diferencia > 0 ? "Sobrante " : "Faltante ") + System.Math.Abs(diferencia).ToString("C", Cultura);

        DiferenciaText.Foreground = (Brush)FindResource(diferencia == 0 ? "FarmaSuccessBrush" : "FarmaDangerBrush");
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        MensajeText.Text = string.Empty;

        // Validación de tipo de dato: el importe debe ser un número válido
        var contado = ValidacionEntrada.LeerDecimal(EfectivoContadoText.Text);

        if (contado == null)
        {
            MensajeText.Text = "Ingresá el efectivo contado (solo números, con hasta 2 decimales).";
            return;
        }

        if (contado.Value != EfectivoEsperado && string.IsNullOrWhiteSpace(ObservacionesText.Text))
        {
            MensajeText.Text = "Hay diferencia con lo esperado: escribí una observación.";
            return;
        }

        System.Windows.MessageBox.Show("Caja cerrada (maqueta).", "FarmaGest",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

        Close();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => Close();
}
