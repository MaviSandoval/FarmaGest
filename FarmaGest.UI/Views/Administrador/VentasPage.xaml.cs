using System.Collections.Generic;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Administrador;

public partial class VentasPage : Page
{
    public VentasPage()
    {
        InitializeComponent();

        VentasGrid.ItemsSource = new List<VentaFalsa>
        {
            new() { Fecha = "05/09/2026 10:12", TipoVenta = "Con receta", Receta = "Sí", Total = "$ 3.980", Estado = "Facturada" },
            new() { Fecha = "05/09/2026 09:45", TipoVenta = "Mostrador", Receta = "No", Total = "$ 1.250", Estado = "Facturada" },
            new() { Fecha = "04/09/2026 18:30", TipoVenta = "Con receta", Receta = "Sí", Total = "$ 2.150", Estado = "Facturada" },
        };
    }

    private class VentaFalsa
    {
        public string Fecha { get; set; } = "";
        public string TipoVenta { get; set; } = "";
        public string Receta { get; set; } = "";
        public string Total { get; set; } = "";
        public string Estado { get; set; } = "";
    }
}