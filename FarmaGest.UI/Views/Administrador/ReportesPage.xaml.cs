using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Administrador;

// MAQUETA: reportes cruzados con datos de ejemplo. Se conectan a la base más adelante.
public partial class ReportesPage : Page
{
    public ReportesPage()
    {
        InitializeComponent();

        DesdePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        HastaPicker.SelectedDate = DateTime.Today;

        ObraSocialGrid.ItemsSource = new List<ReporteObraSocial>
        {
            new() { ObraSocial = "PAMI",            Ventas = 64, TotalVendido = 612400m, TotalCubierto = 428680m },
            new() { ObraSocial = "IOSCOR",          Ventas = 48, TotalVendido = 455200m, TotalCubierto = 227600m },
            new() { ObraSocial = "OSDE",            Ventas = 31, TotalVendido = 318900m, TotalCubierto = 127560m },
            new() { ObraSocial = "Swiss Medical",   Ventas = 17, TotalVendido = 172300m, TotalCubierto =  86150m },
            new() { ObraSocial = "Sin obra social", Ventas = 92, TotalVendido = 298700m, TotalCubierto =      0m },
        };

        ProductosGrid.ItemsSource = new List<ReporteProducto>
        {
            new() { Puesto = 1, Producto = "Paracetamol 500 mg x 20 comp.",  Categoria = "Analgésicos y Antipiréticos",  Unidades = 142, Total = 305300m },
            new() { Puesto = 2, Producto = "Ibuprofeno 400 mg x 20 comp.",   Categoria = "Antiinflamatorios",            Unidades = 118, Total = 330400m },
            new() { Puesto = 3, Producto = "Losartán 50 mg x 30 comp.",      Categoria = "Cardiovasculares",             Unidades =  76, Total = 673360m },
            new() { Puesto = 4, Producto = "Amoxicilina 500 mg x 16 comp.",  Categoria = "Antibióticos",                 Unidades =  54, Total = 456300m },
            new() { Puesto = 5, Producto = "Loratadina 10 mg x 10 comp.",    Categoria = "Antialérgicos",                Unidades =  49, Total = 142100m },
            new() { Puesto = 6, Producto = "Alcohol en gel 250 ml",          Categoria = "Cuidado Personal",             Unidades =  45, Total =  85500m },
        };

        CierresGrid.ItemsSource = new List<ReporteCierre>
        {
            new() { Fecha = "22/09/2026", Caja = 11, Cajero = "Cajero Sistema", Esperado = 98450m,  Contado = 98450m },
            new() { Fecha = "21/09/2026", Caja = 10, Cajero = "Cajero Sistema", Esperado = 112300m, Contado = 111800m },
            new() { Fecha = "20/09/2026", Caja =  9, Cajero = "Cajero Sistema", Esperado = 87650m,  Contado = 87650m },
            new() { Fecha = "19/09/2026", Caja =  8, Cajero = "Cajero Sistema", Esperado = 103200m, Contado = 103400m },
        };

        FarmaceuticosGrid.ItemsSource = new List<ReporteFarmaceutico>
        {
            new() { Farmaceutico = "Farmacéutico Sistema", Ventas = 252, ConReceta = 160, Anuladas = 4, Total = 1857500m },
        };
    }

    private void Generar_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Acá se generarían los reportes del período elegido (maqueta).", "FarmaGest",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private void Exportar_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Acá se exportaría el reporte a Excel o PDF (maqueta).", "FarmaGest",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private class ReporteObraSocial
    {
        public string ObraSocial { get; set; } = "";
        public int Ventas { get; set; }
        public decimal TotalVendido { get; set; }
        public decimal TotalCubierto { get; set; }
        public decimal PagadoCliente => TotalVendido - TotalCubierto;
    }

    private class ReporteProducto
    {
        public int Puesto { get; set; }
        public string Producto { get; set; } = "";
        public string Categoria { get; set; } = "";
        public int Unidades { get; set; }
        public decimal Total { get; set; }
    }

    private class ReporteCierre
    {
        public string Fecha { get; set; } = "";
        public int Caja { get; set; }
        public string Cajero { get; set; } = "";
        public decimal Esperado { get; set; }
        public decimal Contado { get; set; }
        public decimal Diferencia => Contado - Esperado;

        // Verde si el arqueo cerró justo, rojo si hubo sobrante o faltante
        public string Estado => Diferencia == 0 ? "Sin diferencia" : "Con diferencia";
    }

    private class ReporteFarmaceutico
    {
        public string Farmaceutico { get; set; } = "";
        public int Ventas { get; set; }
        public int ConReceta { get; set; }
        public int Anuladas { get; set; }
        public decimal Total { get; set; }
    }
}
