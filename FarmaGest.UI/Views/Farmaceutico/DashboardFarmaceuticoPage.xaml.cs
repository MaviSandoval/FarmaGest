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
        }
    }
}