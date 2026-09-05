using System.Collections.Generic;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Administrador;

public partial class RecetasPage : Page
{
    public RecetasPage()
    {
        InitializeComponent();

        RecetasGrid.ItemsSource = new List<RecetaFalsa>
        {
            new() { Afiliado = "Juan Pérez", Medico = "Dr. Alfredo Gómez", FechaEmision = "05/09/2026", FechaVencimiento = "05/10/2026", Estado = "Pendiente" },
            new() { Afiliado = "María López", Medico = "Dra. Carla Ruiz", FechaEmision = "04/09/2026", FechaVencimiento = "04/10/2026", Estado = "Pendiente" },
        };
    }

    private class RecetaFalsa
    {
        public string Afiliado { get; set; } = "";
        public string Medico { get; set; } = "";
        public string FechaEmision { get; set; } = "";
        public string FechaVencimiento { get; set; } = "";
        public string Estado { get; set; } = "";
    }
}