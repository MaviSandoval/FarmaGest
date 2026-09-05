using System.Collections.Generic;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Administrador;

public partial class StockPage : Page
{
    public StockPage()
    {
        InitializeComponent();

        StockGrid.ItemsSource = new List<StockFalso>
        {
            new() { Descripcion = "Amoxicilina 500 mg x 16", Categoria = "Antibióticos", Stock = "8", EstadoStock = "Bajo" },
            new() { Descripcion = "Losartán 50 mg x 30", Categoria = "Cardiovasculares", Stock = "30", EstadoStock = "Normal" },
            new() { Descripcion = "Paracetamol 500 mg x 20", Categoria = "Analgésicos", Stock = "120", EstadoStock = "Normal" },
        };
    }

    private class StockFalso
    {
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Stock { get; set; } = "";
        public string EstadoStock { get; set; } = "";
    }
}