using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero;

// MAQUETA de PreventasPage: mismas columnas y el mismo flujo de cobro, con datos de ejemplo.
// No guarda nada en la base. Usa la ventana de cobro real (CobrarVentaWindow) y el ticket de ejemplo.
public partial class PreventasMaquetaPage : Page
{
    public PreventasMaquetaPage()
    {
        InitializeComponent();

        PreventasGrid.ItemsSource = new List<PreventaFalsa>
        {
            new() { Hora = "11:24", Numero = 1046, Responsable = "Farmacéutico Sistema", Tipo = "Con receta",  Items = 3, Total = 9770.00m },
            new() { Hora = "11:12", Numero = 1045, Responsable = "Farmacéutico Sistema", Tipo = "Con receta",  Items = 2, Total = 9870.40m },
            new() { Hora = "11:02", Numero = 1044, Responsable = "Farmacéutico Sistema", Tipo = "Venta libre", Items = 3, Total = 6240.00m },
        };
    }

    private void Cobrar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PreventaFalsa venta })
            return;

        // 1. Elegir el medio de pago (ventana real de cobro)
        var cobro = new CobrarVentaWindow(venta.Numero, venta.Total) { Owner = Window.GetWindow(this) };

        if (cobro.ShowDialog() != true)
            return;

        // 2. Mostrar el ticket (ejemplo)
        var ticket = new TicketWindow { Owner = Window.GetWindow(this) };
        ticket.ShowDialog();
    }

    private class PreventaFalsa
    {
        public string Hora { get; set; } = "";
        public int Numero { get; set; }
        public string Responsable { get; set; } = "";
        public string Tipo { get; set; } = "";
        public int Items { get; set; }
        public decimal Total { get; set; }
        public string Estado => "Pendiente de cobro";
    }
}
