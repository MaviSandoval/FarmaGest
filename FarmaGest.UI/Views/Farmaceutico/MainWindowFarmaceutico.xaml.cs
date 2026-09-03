using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;

namespace FarmaGest.UI.Views.Farmaceutico
{
    public partial class MainWindowFarmaceutico : FluentWindow
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DispatcherTimer _relojTimer;

        public MainWindowFarmaceutico(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;

            // ---- Vista inicial: Dashboard del Farmacéutico ----
            RootFrame.Navigate(_serviceProvider.GetRequiredService<DashboardFarmaceuticoPage>());

            // ---- Reloj de la barra superior ----
            _relojTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _relojTimer.Tick += (_, _) => ActualizarFechaHora();
            _relojTimer.Start();
            ActualizarFechaHora();

            Closed += (_, _) => _relojTimer.Stop();
        }

        private void ActualizarFechaHora()
        {
            var cultura = new CultureInfo("es-AR");
            var texto = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy - HH:mm", cultura);
            FechaHoraText.Text = char.ToUpper(texto[0], cultura) + texto[1..];
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioButton)
            {
                return;
            }

            switch (radioButton.Name)
            {
                case nameof(NavInicio):
                    RootFrame.Navigate(_serviceProvider.GetRequiredService<DashboardFarmaceuticoPage>());
                    break;

                // TODO: reemplazar cada caso por su Page real a medida que se vayan
                // implementando el resto de las vistas del Farmacéutico.
                case nameof(NavRecetas):
                    MostrarVistaPendiente("Validación de recetas");
                    break;

                case nameof(NavVentas):
                    MostrarVistaPendiente("Ventas");
                    break;

                case nameof(NavProductos):
                    MostrarVistaPendiente("Productos");
                    break;

                case nameof(NavStock):
                    MostrarVistaPendiente("Stock");
                    break;

                case nameof(NavObrasSociales):
                    MostrarVistaPendiente("Obras Sociales");
                    break;
            }
        }

        private void MostrarVistaPendiente(string nombreModulo)
        {
            // Placeholder temporal hasta que se implemente cada Page real.
            RootFrame.Content = new System.Windows.Controls.TextBlock
            {
                Text = $"Módulo '{nombreModulo}' — vista en construcción.",
                FontSize = 16,
                Margin = new Thickness(32),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
        }

        private void CerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = _serviceProvider.GetRequiredService<AuthWindow>();
            authWindow.Show();
            Close();
        }
    }
}