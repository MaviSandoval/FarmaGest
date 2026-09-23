using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Farmaceutico;

// MAQUETA: control de stock de solo lectura para el farmacéutico.
public partial class StockFarmaceuticoPage : Page
{
    public StockFarmaceuticoPage()
    {
        InitializeComponent();

        var productos = new List<StockFalso>
        {
            new() { Descripcion = "Amoxicilina 500 mg x 16", Categoria = "Antibióticos", Stock = 8, StockMinimo = 10 },
            new() { Descripcion = "Losartán 50 mg x 30", Categoria = "Cardiovasculares", Stock = 30, StockMinimo = 10 },
            new() { Descripcion = "Salbutamol 100 mcg x 200 dosis", Categoria = "Antigripales y Respiratorios", Stock = 4, StockMinimo = 5 },
            new() { Descripcion = "Paracetamol 500 mg x 20", Categoria = "Analgésicos y Antipiréticos", Stock = 120, StockMinimo = 20 },
            new() { Descripcion = "Ibuprofeno 400 mg x 20", Categoria = "Antiinflamatorios", Stock = 35, StockMinimo = 15 },
            new() { Descripcion = "Cetirizina 10 mg x 10", Categoria = "Antialérgicos", Stock = 0, StockMinimo = 5 },
            new() { Descripcion = "Alcohol en gel 250 ml", Categoria = "Antigripales y Respiratorios", Stock = 40, StockMinimo = 10 },
        };

        StockGrid.ItemsSource = productos;

        ActivosText.Text = productos.Count.ToString();
        NormalText.Text = productos.Count(p => p.Estado == "Normal").ToString();
        BajoText.Text = productos.Count(p => p.Estado == "Bajo").ToString();
        SinStockText.Text = productos.Count(p => p.Estado == "Sin stock").ToString();
    }

    private class StockFalso
    {
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public int Stock { get; set; }
        public int StockMinimo { get; set; }

        public string Estado =>
            Stock == 0 ? "Sin stock" :
            Stock < StockMinimo ? "Bajo" : "Normal";
    }
}
