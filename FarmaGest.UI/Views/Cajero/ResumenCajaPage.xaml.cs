using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero;

// MAQUETA: resumen de la caja abierta con datos de ejemplo.
public partial class ResumenCajaPage : Page
{
    public ResumenCajaPage()
    {
        InitializeComponent();

        MediosPagoList.ItemsSource = new List<MedioPagoFalso>
        {
            new() { Medio = "Efectivo",      Cantidad = 8, Importe = 84320.00m },
            new() { Medio = "Débito",        Cantidad = 5, Importe = 52180.00m },
            new() { Medio = "Crédito",       Cantidad = 3, Importe = 36950.00m },
            new() { Medio = "Transferencia", Cantidad = 2, Importe = 13000.00m },
        };

        MovimientosGrid.ItemsSource = new List<MovimientoFalso>
        {
            new() { Hora = "11:08", Numero = 1043, Responsable = "Farmacéutico Sistema", Medio = "Efectivo",      Importe = 5316.00m },
            new() { Hora = "10:52", Numero = 1042, Responsable = "Farmacéutico Sistema", Medio = "Débito",        Importe = 2150.00m },
            new() { Hora = "10:31", Numero = 1040, Responsable = "Farmacéutico Sistema", Medio = "Crédito",       Importe = 7425.00m },
            new() { Hora = "10:05", Numero = 1039, Responsable = "Farmacéutico Sistema", Medio = "Efectivo",      Importe = 12900.00m },
            new() { Hora = "09:40", Numero = 1037, Responsable = "Farmacéutico Sistema", Medio = "Transferencia", Importe = 8500.00m },
            new() { Hora = "09:12", Numero = 1035, Responsable = "Farmacéutico Sistema", Medio = "Débito",        Importe = 14360.00m },
            new() { Hora = "08:25", Numero = 1033, Responsable = "Farmacéutico Sistema", Medio = "Efectivo",      Importe = 3100.00m },
        };
    }

    private void CerrarCaja_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new CierreCajaWindow { Owner = Window.GetWindow(this) };
        ventana.ShowDialog();
    }

    private void VerTicket_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new TicketWindow { Owner = Window.GetWindow(this) };
        ventana.ShowDialog();
    }

    private void Imprimir_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Acá se imprimiría el resumen de caja (maqueta).", "FarmaGest",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private class MedioPagoFalso
    {
        public string Medio { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Importe { get; set; }
    }

    private class MovimientoFalso
    {
        public string Hora { get; set; } = "";
        public int Numero { get; set; }
        public string Responsable { get; set; } = "";
        public string Medio { get; set; } = "";
        public decimal Importe { get; set; }
    }
}
