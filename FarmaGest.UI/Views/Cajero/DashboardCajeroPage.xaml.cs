using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Cajero;

// MAQUETA: inicio del Cajero con datos de ejemplo. Se conecta al backend más adelante.
public partial class DashboardCajeroPage : Page
{
    public DashboardCajeroPage()
    {
        InitializeComponent();

        PreventasGrid.ItemsSource = new List<PreventaFalsa>
        {
            new() { Numero = 1046, Hora = "11:24", Responsable = "Farmacéutico Sistema", Items = 3, Total = 9770.00m },
            new() { Numero = 1045, Hora = "11:12", Responsable = "Farmacéutico Sistema", Items = 2, Total = 9870.40m },
            new() { Numero = 1044, Hora = "11:02", Responsable = "Farmacéutico Sistema", Items = 3, Total = 6240.00m },
        };

        // El ancho de cada barra es proporcional al medio de pago con más importe (máx. 260 px)
        const double anchoMaximo = 260;
        const decimal mayorImporte = 84320.00m;

        MediosPagoList.ItemsSource = new List<MedioPagoFalso>
        {
            new() { Medio = "Efectivo",      Cantidad = 8, Importe = 84320.00m },
            new() { Medio = "Débito",        Cantidad = 5, Importe = 52180.00m },
            new() { Medio = "Crédito",       Cantidad = 3, Importe = 36950.00m },
            new() { Medio = "Transferencia", Cantidad = 2, Importe = 13000.00m },
        }.ConvertAll(m =>
        {
            m.AnchoBarra = (double)(m.Importe / mayorImporte) * anchoMaximo;
            return m;
        });
    }

    private void VerResumen_Click(object sender, RoutedEventArgs e) =>
        NavigationService?.Navigate(new ResumenCajaPage());

    private class PreventaFalsa
    {
        public int Numero { get; set; }
        public string Hora { get; set; } = "";
        public string Responsable { get; set; } = "";
        public int Items { get; set; }
        public decimal Total { get; set; }
        public string Estado => "Pendiente de cobro";
    }

    private class MedioPagoFalso
    {
        public string Medio { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Importe { get; set; }
        public double AnchoBarra { get; set; }
    }
}
