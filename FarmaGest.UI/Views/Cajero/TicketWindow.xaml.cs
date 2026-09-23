using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Wpf.Ui.Controls;

namespace FarmaGest.UI.Views.Cajero;

// MAQUETA: ticket de ejemplo (venta N° 1046). Más adelante recibirá los datos reales de la venta cobrada.
public partial class TicketWindow : FluentWindow
{
    private static readonly CultureInfo Cultura = new("es-AR");

    public TicketWindow()
    {
        InitializeComponent();

        var detalle = new List<ItemTicketFalso>
        {
            new() { Producto = "Amoxicilina 500 mg x 16 comp.", Cantidad = 1, PrecioUnitario = 8450.00m, PorcentajeCobertura = 40 },
            new() { Producto = "Ibuprofeno 400 mg x 20 comp.",  Cantidad = 1, PrecioUnitario = 2800.00m },
            new() { Producto = "Alcohol en gel 250 ml",         Cantidad = 1, PrecioUnitario = 1900.00m },
        };

        DetalleList.ItemsSource = detalle;

        decimal subtotal = detalle.Sum(i => i.Subtotal);
        decimal descuento = detalle.Sum(i => i.Descuento);

        SubtotalText.Text = subtotal.ToString("C", Cultura);
        DescuentoText.Text = "- " + descuento.ToString("C", Cultura);
        TotalText.Text = (subtotal - descuento).ToString("C", Cultura);
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();

    private void Imprimir_Click(object sender, RoutedEventArgs e) =>
        System.Windows.MessageBox.Show("Acá se enviaría el ticket a la impresora (maqueta).", "FarmaGest",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

    private class ItemTicketFalso
    {
        public string Producto { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeCobertura { get; set; }

        public decimal Subtotal => Cantidad * PrecioUnitario;
        public decimal Descuento => Subtotal * PorcentajeCobertura / 100m;
        public bool TieneDescuento => Descuento > 0;
        public string Linea => $"{Cantidad} x {PrecioUnitario.ToString("N2", Cultura)}";
        public string TextoDescuento => $"  Cobertura {PorcentajeCobertura:0}%";
    }
}
