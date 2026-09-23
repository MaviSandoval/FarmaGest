using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaGest.UI.Views.Farmaceutico;

// MAQUETA: datos hardcodeados. Se reemplazan por RecetaService cuando se conecte el backend.
public partial class ValidacionRecetasPage : Page
{
    public ValidacionRecetasPage()
    {
        InitializeComponent();

        RecetasGrid.ItemsSource = new List<RecetaFalsa>
        {
            new()
            {
                Numero = 1024, Paciente = "Juan Pérez", NumeroAfiliado = "61-458921-02",
                ObraSocialPlan = "OSDE · Plan 210", Medico = "Dr. Alfredo Gómez", Matricula = "MP 4521",
                FechaEmision = new DateTime(2026, 9, 23), FechaVencimiento = new DateTime(2026, 10, 23),
                Estado = "Pendiente",
                Medicamentos =
                {
                    new() { Producto = "Amoxicilina 500 mg x 16", Cantidad = 1, CoberturaTexto = "40 %" },
                    new() { Producto = "Ibuprofeno 400 mg x 20", Cantidad = 1, CoberturaTexto = "Sin cobertura" },
                },
                Verificaciones =
                {
                    new() { Texto = "Receta vigente", Ok = true },
                    new() { Texto = "Afiliado activo", Ok = true },
                    new() { Texto = "Ibuprofeno sin cobertura en el plan", Ok = false },
                    new() { Texto = "Stock disponible", Ok = true },
                }
            },
            new()
            {
                Numero = 1023, Paciente = "María López", NumeroAfiliado = "15-332190-00",
                ObraSocialPlan = "IOSCOR · Plan General", Medico = "Dra. Carla Ruiz", Matricula = "MP 3310",
                FechaEmision = new DateTime(2026, 9, 22), FechaVencimiento = new DateTime(2026, 10, 22),
                Estado = "Pendiente",
                Medicamentos =
                {
                    new() { Producto = "Losartán 50 mg x 30", Cantidad = 2, CoberturaTexto = "70 %" },
                },
                Verificaciones =
                {
                    new() { Texto = "Receta vigente", Ok = true },
                    new() { Texto = "Afiliado activo", Ok = true },
                    new() { Texto = "Medicamentos con cobertura", Ok = true },
                    new() { Texto = "Stock disponible", Ok = true },
                }
            },
            new()
            {
                Numero = 1022, Paciente = "Carlos Benítez", NumeroAfiliado = "08-771204-01",
                ObraSocialPlan = "Swiss Medical · SMG20", Medico = "Dr. Nicolás Paz", Matricula = "MP 5102",
                FechaEmision = new DateTime(2026, 9, 21), FechaVencimiento = new DateTime(2026, 10, 21),
                Estado = "Validada",
                Medicamentos =
                {
                    new() { Producto = "Salbutamol 100 mcg x 200 dosis", Cantidad = 1, CoberturaTexto = "50 %" },
                },
                Verificaciones =
                {
                    new() { Texto = "Receta vigente", Ok = true },
                    new() { Texto = "Afiliado activo", Ok = true },
                    new() { Texto = "Medicamentos con cobertura", Ok = true },
                    new() { Texto = "Stock disponible", Ok = true },
                }
            },
            new()
            {
                Numero = 1019, Paciente = "Ana Romero", NumeroAfiliado = "22-104556-03",
                ObraSocialPlan = "PAMI · Plan Único", Medico = "Dra. Laura Sosa", Matricula = "MP 2877",
                FechaEmision = new DateTime(2026, 8, 10), FechaVencimiento = new DateTime(2026, 9, 9),
                Estado = "Rechazada",
                Medicamentos =
                {
                    new() { Producto = "Cetirizina 10 mg x 10", Cantidad = 1, CoberturaTexto = "30 %" },
                },
                Verificaciones =
                {
                    new() { Texto = "Receta vencida el 09/09/2026", Ok = false },
                    new() { Texto = "Afiliado activo", Ok = true },
                    new() { Texto = "Medicamentos con cobertura", Ok = true },
                    new() { Texto = "Stock disponible", Ok = true },
                }
            },
        };

        RecetasGrid.SelectedIndex = 0;
    }

    private void RegistrarReceta_Click(object sender, RoutedEventArgs e)
    {
        var ventana = new RegistrarRecetaWindow { Owner = Window.GetWindow(this) };
        ventana.ShowDialog();
    }

    private void Validar_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Receta validada (maqueta).", "FarmaGest",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private void Rechazar_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Receta rechazada (maqueta).", "FarmaGest",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private void IniciarVenta_Click(object sender, RoutedEventArgs e) =>
        NavigationService?.Navigate(new NuevaVentaPage());

    private class RecetaFalsa
    {
        public int Numero { get; set; }
        public string Paciente { get; set; } = "";
        public string NumeroAfiliado { get; set; } = "";
        public string ObraSocialPlan { get; set; } = "";
        public string Medico { get; set; } = "";
        public string Matricula { get; set; } = "";
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; } = "";
        public List<MedicamentoFalso> Medicamentos { get; } = new();
        public List<VerificacionFalsa> Verificaciones { get; } = new();
    }

    private class MedicamentoFalso
    {
        public string Producto { get; set; } = "";
        public int Cantidad { get; set; }
        public string CoberturaTexto { get; set; } = "";
    }

    private class VerificacionFalsa
    {
        public string Texto { get; set; } = "";
        public bool Ok { get; set; }
    }
}
