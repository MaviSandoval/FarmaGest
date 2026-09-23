using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.UI.Views.Cajero;

namespace FarmaGest.UI.Views.Administrador;

// MAQUETA: historial de ventas con datos de ejemplo.
public partial class VentasPage : Page
{
    public VentasPage()
    {
        InitializeComponent();

        FechaPicker.SelectedDate = DateTime.Today;

        VentasGrid.ItemsSource = new List<VentaFalsa>
        {
            new() { Numero = 1046, Hora = "11:24", Tipo = "Con receta",  Cliente = "Juan Pérez (OSDE 210)",          MedioPago = "—",             Total = 9770.00m,  Estado = "Pendiente de cobro" },
            new() { Numero = 1045, Hora = "11:12", Tipo = "Con receta",  Cliente = "María López (IOSCOR)",           MedioPago = "—",             Total = 9870.40m,  Estado = "Pendiente de cobro" },
            new() { Numero = 1044, Hora = "11:02", Tipo = "Venta libre", Cliente = "Consumidor final",               MedioPago = "—",             Total = 6240.00m,  Estado = "Pendiente de cobro" },
            new() { Numero = 1043, Hora = "10:41", Tipo = "Con receta",  Cliente = "María López (IOSCOR)",           MedioPago = "Efectivo",      Total = 5316.00m,  Estado = "Facturada" },
            new() { Numero = 1042, Hora = "10:15", Tipo = "Venta libre", Cliente = "Consumidor final",               MedioPago = "Débito",        Total = 2150.00m,  Estado = "Facturada" },
            new() { Numero = 1041, Hora = "09:50", Tipo = "Venta libre", Cliente = "Consumidor final",               MedioPago = "—",             Total = 4380.00m,  Estado = "Anulada" },
            new() { Numero = 1040, Hora = "09:22", Tipo = "Con receta",  Cliente = "Carlos Benítez (Swiss Medical)", MedioPago = "Crédito",       Total = 7425.00m,  Estado = "Facturada" },
            new() { Numero = 1039, Hora = "09:05", Tipo = "Venta libre", Cliente = "Consumidor final",               MedioPago = "Efectivo",      Total = 12900.00m, Estado = "Facturada" },
        };
    }

    private void VerTicket_Click(object sender, RoutedEventArgs e)
    {
        var ticket = new TicketWindow { Owner = Window.GetWindow(this) };
        ticket.ShowDialog();
    }

    private void Exportar_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Acá se exportaría el listado de ventas (maqueta).", "FarmaGest",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private class VentaFalsa
    {
        public int Numero { get; set; }
        public string Hora { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string MedioPago { get; set; } = "";
        public decimal Total { get; set; }
        public string Estado { get; set; } = "";

        // Solo las ventas cobradas tienen ticket
        public bool TieneTicket => Estado == "Facturada";
    }
}
