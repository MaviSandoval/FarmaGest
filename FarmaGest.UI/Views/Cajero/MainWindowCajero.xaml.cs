using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FarmaGest.Negocio.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;

namespace FarmaGest.UI.Views.Cajero
{
    public partial class MainWindowCajero : FluentWindow
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SesionUsuarioService _sesionUsuarioService;
        private readonly DispatcherTimer _relojTimer;

        public MainWindowCajero(
            IServiceProvider serviceProvider,
            SesionUsuarioService sesionUsuarioService)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _sesionUsuarioService = sesionUsuarioService;

            // ---- Reloj de la barra superior ----
            _relojTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _relojTimer.Tick += (_, _) => ActualizarFechaHora();
            _relojTimer.Start();

            ActualizarFechaHora();

            Closed += (_, _) => _relojTimer.Stop();
        }

        private void ActualizarFechaHora()
        {
            var cultura = new CultureInfo("es-AR");

            var texto = DateTime.Now.ToString(
                "dddd, dd 'de' MMMM 'de' yyyy - HH:mm",
                cultura);

            FechaHoraText.Text =
                char.ToUpper(texto[0], cultura) + texto[1..];
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioButton)
                return;

            switch (radioButton.Name)
            {
                case nameof(NavInicio):
                    MostrarVistaPendiente("Inicio");
                    break;

                case nameof(NavPreventas):
                    RootFrame.Navigate(
                        _serviceProvider.GetRequiredService<PreventasPage>());
                    break;

                case nameof(NavVentasCobradas):
                    MostrarVistaPendiente("Ventas cobradas");
                    break;

                case nameof(NavCaja):
                    RootFrame.Navigate(
                        _serviceProvider.GetRequiredService<CajaPage>());
                    break;

                case nameof(NavResumen):
                    MostrarVistaPendiente("Resumen de caja");
                    break;
            }
        }

        private void MostrarVistaPendiente(string nombreModulo)
        {
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
            _sesionUsuarioService.CerrarSesion();

            var authWindow =
                _serviceProvider.GetRequiredService<AuthWindow>();

            authWindow.Show();
            Close();
        }
    }
}