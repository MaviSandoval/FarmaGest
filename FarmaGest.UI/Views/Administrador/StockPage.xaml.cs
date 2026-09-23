using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Administrador;

// MAQUETA: control de stock con los productos de prueba del script de la base.
public partial class StockPage : Page
{
    public StockPage()
    {
        InitializeComponent();

        var productos = new List<StockFalso>
        {
            new() { Descripcion = "Amoxicilina 500 mg x 16 comp.",          Categoria = "Antibióticos",                 Stock = 8,   StockMinimo = 10 },
            new() { Descripcion = "Cefalexina 500 mg x 16 comp.",           Categoria = "Antibióticos",                 Stock = 4,   StockMinimo = 10 },
            new() { Descripcion = "Salbutamol aerosol 100 mcg x 200 dosis", Categoria = "Antigripales y Respiratorios", Stock = 4,   StockMinimo = 5 },
            new() { Descripcion = "Atorvastatina 20 mg x 30 comp.",         Categoria = "Cardiovasculares",             Stock = 3,   StockMinimo = 10 },
            new() { Descripcion = "Cetirizina 10 mg x 10 comp.",            Categoria = "Antialérgicos",                Stock = 0,   StockMinimo = 5 },
            new() { Descripcion = "Termómetro digital",                     Categoria = "Cuidado Personal",             Stock = 0,   StockMinimo = 3 },
            new() { Descripcion = "Losartán 50 mg x 30 comp.",              Categoria = "Cardiovasculares",             Stock = 30,  StockMinimo = 10 },
            new() { Descripcion = "Paracetamol 500 mg x 20 comp.",          Categoria = "Analgésicos y Antipiréticos",  Stock = 120, StockMinimo = 20 },
            new() { Descripcion = "Ibuprofeno 400 mg x 20 comp.",           Categoria = "Antiinflamatorios",            Stock = 35,  StockMinimo = 15 },
            new() { Descripcion = "Alcohol en gel 250 ml",                  Categoria = "Cuidado Personal",             Stock = 40,  StockMinimo = 10 },
        };

        StockGrid.ItemsSource = productos;

        ActivosText.Text = "24";
        NormalText.Text = (24 - productos.Count(p => p.Estado != "Normal")).ToString();
        BajoText.Text = productos.Count(p => p.Estado == "Bajo").ToString();
        SinStockText.Text = productos.Count(p => p.Estado == "Sin stock").ToString();
    }

    private void AjustarStock_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Acá se registraría un ingreso de mercadería o un ajuste por rotura/vencimiento (maqueta).",
            "FarmaGest", MessageBoxButton.OK, MessageBoxImage.Information);

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
