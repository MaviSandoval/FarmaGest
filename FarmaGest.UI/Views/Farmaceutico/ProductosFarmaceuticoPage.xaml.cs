using System.Collections.Generic;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Farmaceutico;

// MAQUETA: catálogo de solo lectura para el farmacéutico.
public partial class ProductosFarmaceuticoPage : Page
{
    public ProductosFarmaceuticoPage()
    {
        InitializeComponent();

        ProductosGrid.ItemsSource = new List<ProductoFalso>
        {
            new()
            {
                Descripcion = "Amoxicilina 500 mg x 16", Categoria = "Antibióticos", Precio = 8450m, Stock = 8,
                RequiereReceta = true, Estado = "Activo",
                Coberturas = { new() { Plan = "OSDE · Plan 210", Porcentaje = 40 }, new() { Plan = "IOSCOR · General", Porcentaje = 50 }, new() { Plan = "PAMI · Único", Porcentaje = 80 } }
            },
            new()
            {
                Descripcion = "Losartán 50 mg x 30", Categoria = "Cardiovasculares", Precio = 8860m, Stock = 30,
                RequiereReceta = true, Estado = "Activo",
                Coberturas = { new() { Plan = "IOSCOR · General", Porcentaje = 70 }, new() { Plan = "PAMI · Único", Porcentaje = 100 } }
            },
            new()
            {
                Descripcion = "Salbutamol 100 mcg x 200 dosis", Categoria = "Antigripales y Respiratorios", Precio = 14850m, Stock = 12,
                RequiereReceta = true, Estado = "Activo",
                Coberturas = { new() { Plan = "Swiss Medical · SMG20", Porcentaje = 50 } }
            },
            new()
            {
                Descripcion = "Paracetamol 500 mg x 20", Categoria = "Analgésicos y Antipiréticos", Precio = 2150m, Stock = 120,
                RequiereReceta = false, Estado = "Activo"
            },
            new()
            {
                Descripcion = "Ibuprofeno 400 mg x 20", Categoria = "Antiinflamatorios", Precio = 2800m, Stock = 35,
                RequiereReceta = false, Estado = "Activo"
            },
            new()
            {
                Descripcion = "Cetirizina 10 mg x 10", Categoria = "Antialérgicos", Precio = 3100m, Stock = 0,
                RequiereReceta = false, Estado = "Inactivo",
                Coberturas = { new() { Plan = "PAMI · Único", Porcentaje = 30 } }
            },
        };

        ProductosGrid.SelectedIndex = 0;
    }

    private class ProductoFalso
    {
        public string Descripcion { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public bool RequiereReceta { get; set; }
        public string Estado { get; set; } = "";
        public List<CoberturaFalsa> Coberturas { get; } = new();

        public string RequiereRecetaTexto => RequiereReceta ? "Sí" : "No";
    }

    private class CoberturaFalsa
    {
        public string Plan { get; set; } = "";
        public decimal Porcentaje { get; set; }
    }
}
