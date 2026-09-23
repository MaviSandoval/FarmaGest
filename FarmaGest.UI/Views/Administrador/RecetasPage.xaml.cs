using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Administrador;

// MAQUETA: consulta de recetas (solo lectura para el Administrador).
public partial class RecetasPage : Page
{
    public RecetasPage()
    {
        InitializeComponent();

        RecetasGrid.ItemsSource = new List<RecetaFalsa>
        {
            new() { Numero = 1024, Afiliado = "Juan Pérez",     ObraSocial = "OSDE · Plan 210",       Medico = "Dr. Alfredo Gómez", FechaEmision = "23/09/2026", FechaVencimiento = "23/10/2026", Valido = "—",                    Estado = "Pendiente" },
            new() { Numero = 1023, Afiliado = "María López",    ObraSocial = "IOSCOR · General",      Medico = "Dra. Carla Ruiz",   FechaEmision = "22/09/2026", FechaVencimiento = "22/10/2026", Valido = "—",                    Estado = "Pendiente" },
            new() { Numero = 1022, Afiliado = "Carlos Benítez", ObraSocial = "Swiss Medical · SMG20", Medico = "Dr. Nicolás Paz",   FechaEmision = "21/09/2026", FechaVencimiento = "21/10/2026", Valido = "Farmacéutico Sistema", Estado = "Validada" },
            new() { Numero = 1021, Afiliado = "Rosa Acosta",    ObraSocial = "PAMI · Único",          Medico = "Dra. Laura Sosa",   FechaEmision = "20/09/2026", FechaVencimiento = "20/10/2026", Valido = "Farmacéutico Sistema", Estado = "Validada" },
            new() { Numero = 1019, Afiliado = "Ana Romero",     ObraSocial = "PAMI · Único",          Medico = "Dra. Laura Sosa",   FechaEmision = "10/08/2026", FechaVencimiento = "09/09/2026", Valido = "Farmacéutico Sistema", Estado = "Rechazada" },
        };
    }

    private void VerDetalle_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Acá se abriría el detalle de la receta con sus medicamentos (maqueta).", "FarmaGest",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private class RecetaFalsa
    {
        public int Numero { get; set; }
        public string Afiliado { get; set; } = "";
        public string ObraSocial { get; set; } = "";
        public string Medico { get; set; } = "";
        public string FechaEmision { get; set; } = "";
        public string FechaVencimiento { get; set; } = "";
        public string Valido { get; set; } = "";
        public string Estado { get; set; } = "";
    }
}
