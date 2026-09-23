using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Farmaceutico;

// MAQUETA: listado de ventas del farmacéutico con datos hardcodeados.
public partial class VentasFarmaceuticoPage : Page
{
    public VentasFarmaceuticoPage()
    {
        InitializeComponent();

        FechaFiltroPicker.SelectedDate = DateTime.Today;
        var hoy = DateTime.Today;

        VentasGrid.ItemsSource = new List<VentaFalsa>
        {
            new() { Numero = 1045, Fecha = hoy.AddHours(11).AddMinutes(12), Tipo = "Con receta", Cliente = "Juan Pérez (OSDE 210)", Items = 2, Total = 9870.40m, Estado = "Pendiente de cobro" },
            new() { Numero = 1044, Fecha = hoy.AddHours(11).AddMinutes(2),  Tipo = "Venta libre", Cliente = "Consumidor final", Items = 3, Total = 6240.00m, Estado = "Pendiente de cobro" },
            new() { Numero = 1043, Fecha = hoy.AddHours(10).AddMinutes(41), Tipo = "Con receta", Cliente = "María López (IOSCOR)", Items = 1, Total = 5316.00m, Estado = "Facturada" },
            new() { Numero = 1042, Fecha = hoy.AddHours(10).AddMinutes(15), Tipo = "Venta libre", Cliente = "Consumidor final", Items = 1, Total = 2150.00m, Estado = "Facturada" },
            new() { Numero = 1041, Fecha = hoy.AddHours(9).AddMinutes(50),  Tipo = "Venta libre", Cliente = "Consumidor final", Items = 2, Total = 4380.00m, Estado = "Anulada" },
            new() { Numero = 1040, Fecha = hoy.AddHours(9).AddMinutes(22),  Tipo = "Con receta", Cliente = "Carlos Benítez (Swiss Medical)", Items = 1, Total = 7425.00m, Estado = "Facturada" },
        };
    }

    private void NuevaVenta_Click(object sender, RoutedEventArgs e) =>
        NavigationService?.Navigate(new NuevaVentaPage());

    private void VerDetalle_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Acá se abriría el detalle de la venta (maqueta).", "FarmaGest",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private void Anular_Click(object sender, RoutedEventArgs e)
    {
        var respuesta = MessageBox.Show("¿Anular esta venta pendiente de cobro?", "FarmaGest",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (respuesta == MessageBoxResult.Yes)
        {
            MessageBox.Show("Venta anulada (maqueta).", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private class VentaFalsa
    {
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = "";
        public string Cliente { get; set; } = "";
        public int Items { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = "";

        // Solo se puede anular mientras el cajero no la cobró
        public bool PuedeAnular => Estado == "Pendiente de cobro";
    }
}
