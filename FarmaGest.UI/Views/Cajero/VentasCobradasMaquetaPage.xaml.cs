using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero;

// MAQUETA de VentasCobradasPage con datos de ejemplo.
public partial class VentasCobradasMaquetaPage : Page
{
    public VentasCobradasMaquetaPage()
    {
        InitializeComponent();

        FechaPicker.SelectedDate = DateTime.Today;

        VentasGrid.ItemsSource = new List<VentaCobradaFalsa>
        {
            new() { Hora = "11:08", Numero = 1043, MetodoPago = "Efectivo",      Importe = 5316.00m },
            new() { Hora = "10:52", Numero = 1042, MetodoPago = "Débito",        Importe = 2150.00m },
            new() { Hora = "10:31", Numero = 1040, MetodoPago = "Crédito",       Importe = 7425.00m },
            new() { Hora = "10:05", Numero = 1039, MetodoPago = "Efectivo",      Importe = 12900.00m },
            new() { Hora = "09:40", Numero = 1037, MetodoPago = "Transferencia", Importe = 8500.00m },
            new() { Hora = "09:12", Numero = 1035, MetodoPago = "Débito",        Importe = 14360.00m },
            new() { Hora = "08:25", Numero = 1033, MetodoPago = "Efectivo",      Importe = 3100.00m },
        };
    }

    private void VerTicket_Click(object sender, RoutedEventArgs e)
    {
        var ticket = new TicketWindow { Owner = Window.GetWindow(this) };
        ticket.ShowDialog();
    }

    private class VentaCobradaFalsa
    {
        public string Hora { get; set; } = "";
        public int Numero { get; set; }
        public string Responsable { get; set; } = "Farmacéutico Sistema";
        public string MetodoPago { get; set; } = "";
        public decimal Importe { get; set; }
        public string Cajero { get; set; } = "Cajero Sistema";
        public string Estado => "Facturada";
    }
}
