using System;
using System.Collections.Generic;
using System.Windows;
using Wpf.Ui.Controls;

namespace FarmaGest.UI.Views.Farmaceutico;

// MAQUETA: ventana de alta de receta con datos de ejemplo.
public partial class RegistrarRecetaWindow : FluentWindow
{
    public RegistrarRecetaWindow()
    {
        InitializeComponent();

        FechaEmisionPicker.SelectedDate = DateTime.Today;
        FechaVencimientoPicker.SelectedDate = DateTime.Today.AddDays(30);

        MedicamentosGrid.ItemsSource = new List<MedicamentoFalso>
        {
            new() { Producto = "Amoxicilina 500 mg x 16", Cantidad = 1, CoberturaTexto = "40 %" },
            new() { Producto = "Ibuprofeno 400 mg x 20", Cantidad = 1, CoberturaTexto = "Sin cobertura" },
        };
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Receta registrada (maqueta).", "FarmaGest",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private class MedicamentoFalso
    {
        public string Producto { get; set; } = "";
        public int Cantidad { get; set; }
        public string CoberturaTexto { get; set; } = "";
    }
}
