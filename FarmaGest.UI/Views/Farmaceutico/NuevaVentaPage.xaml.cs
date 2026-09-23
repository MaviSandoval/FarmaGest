using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Farmaceutico;

// MAQUETA: el farmacéutico arma la venta (productos + receta + cobertura) y la envía a caja.
public partial class NuevaVentaPage : Page
{
    private static readonly CultureInfo Cultura = new("es-AR");

    public NuevaVentaPage()
    {
        InitializeComponent();

        var detalle = new List<ItemVentaFalso>
        {
            new() { Producto = "Amoxicilina 500 mg x 16", Origen = "Receta", Cantidad = 1, PrecioUnitario = 8450.00m, PorcentajeCobertura = 40 },
            new() { Producto = "Ibuprofeno 400 mg x 20", Origen = "Receta", Cantidad = 1, PrecioUnitario = 2800.00m, PorcentajeCobertura = 0 },
            new() { Producto = "Alcohol en gel 250 ml", Origen = "Libre", Cantidad = 1, PrecioUnitario = 1900.00m, PorcentajeCobertura = 0 },
        };

        DetalleGrid.ItemsSource = detalle;

        // Totales calculados a partir del detalle
        decimal subtotal = detalle.Sum(i => i.Cantidad * i.PrecioUnitario);
        decimal descuento = detalle.Sum(i => i.Descuento);

        CantidadItemsText.Text = detalle.Sum(i => i.Cantidad).ToString(Cultura);
        SubtotalText.Text = subtotal.ToString("C", Cultura);
        DescuentoText.Text = "- " + descuento.ToString("C", Cultura);
        TotalText.Text = (subtotal - descuento).ToString("C", Cultura);
    }

    private void Volver_Click(object sender, RoutedEventArgs e) =>
        NavigationService?.Navigate(new VentasFarmaceuticoPage());

    private void RegistrarReceta_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new RegistrarRecetaWindow { Owner = Window.GetWindow(this) };
        ventana.ShowDialog();
    }

    private void EnviarACaja_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Venta N° 1046 enviada a caja. Queda pendiente de cobro (maqueta).",
            "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);

        NavigationService?.Navigate(new VentasFarmaceuticoPage());
    }

    private class ItemVentaFalso
    {
        public string Producto { get; set; } = "";
        public string Origen { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeCobertura { get; set; }

        public decimal Descuento => Cantidad * PrecioUnitario * PorcentajeCobertura / 100m;
        public decimal Subtotal => Cantidad * PrecioUnitario - Descuento;
        public string CoberturaTexto => PorcentajeCobertura > 0 ? $"{PorcentajeCobertura:0} %" : "—";
    }
}
