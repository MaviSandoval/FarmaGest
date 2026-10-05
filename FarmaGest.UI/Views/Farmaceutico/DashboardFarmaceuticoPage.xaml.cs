using System;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.UI.ViewModels.Farmaceutico;

namespace FarmaGest.UI.Views.Farmaceutico
{
    public partial class DashboardFarmaceuticoPage : Page
    {
        public DashboardFarmaceuticoPage(DashboardFarmaceuticoViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            Loaded += async (_, _) =>
            {
                try
                {
                    await viewModel.CargarDatosAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudieron cargar los datos de inicio.\n\n{ex.Message}", "FarmaGest",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
        }
    }
}
