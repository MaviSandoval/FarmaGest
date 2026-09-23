using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Views.Compartido;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;

namespace FarmaGest.UI.Views.Farmaceutico
{
    public partial class MainWindowFarmaceutico : FluentWindow
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SesionUsuarioService _sesionUsuarioService;
        private readonly DispatcherTimer _relojTimer;

        public MainWindowFarmaceutico(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _sesionUsuarioService = _serviceProvider.GetRequiredService<SesionUsuarioService>();

            // ---- Vista inicial: Dashboard del Farmacéutico ----
            RootFrame.Navigate(_serviceProvider.GetRequiredService<DashboardFarmaceuticoPage>());

            // ---- Encabezado con el nombre y la foto del usuario logueado ----
            ActualizarEncabezado();
            _sesionUsuarioService.UsuarioActualizado += ActualizarEncabezado;

            // ---- Reloj de la barra superior ----
            _relojTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _relojTimer.Tick += (_, _) => ActualizarFechaHora();
            _relojTimer.Start();
            ActualizarFechaHora();

            Closed += (_, _) =>
            {
                _relojTimer.Stop();
                _sesionUsuarioService.UsuarioActualizado -= ActualizarEncabezado;
            };
        }

        private void ActualizarFechaHora()
        {
            var cultura = new CultureInfo("es-AR");
            var texto = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy - HH:mm", cultura);
            FechaHoraText.Text = char.ToUpper(texto[0], cultura) + texto[1..];
        }

        private void ActualizarEncabezado()
        {
            var usuario = _sesionUsuarioService.UsuarioActual;
            if (usuario == null)
                return;

            BienvenidaText.Text = $"Bienvenido/a, {usuario.Nombre}";
            NombreUsuarioText.Text = $"{usuario.Nombre} {usuario.Apellido}";
            AvatarEncabezado.Mostrar(usuario);
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

                case nameof(NavRecetas):
                    RootFrame.Navigate(new ValidacionRecetasPage());
                    break;

                case nameof(NavVentas):
                    RootFrame.Navigate(new VentasFarmaceuticoPage());
                    break;

                case nameof(NavProductos):
                    RootFrame.Navigate(new ProductosFarmaceuticoPage());
                    break;

                case nameof(NavStock):
                    RootFrame.Navigate(new StockFarmaceuticoPage());
                    break;

                case nameof(NavObrasSociales):
                    RootFrame.Navigate(new ObrasSocialesPage());
                    break;
            }
        }

        private void MiPerfil_Click(object sender, RoutedEventArgs e)
        {
            // Ninguna opción del menú queda marcada mientras se está en Mi perfil
            foreach (var item in NavPanel.Children.OfType<RadioButton>())
                item.IsChecked = false;

            RootFrame.Navigate(_serviceProvider.GetRequiredService<MiPerfilPage>());
        }

        private void CerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var respuesta = System.Windows.MessageBox.Show(
                "¿Querés cerrar la sesión?",
                "Cerrar sesión",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (respuesta != System.Windows.MessageBoxResult.Yes)
                return;

            _sesionUsuarioService.CerrarSesion();

            var authWindow = _serviceProvider.GetRequiredService<AuthWindow>();
            authWindow.Show();
            Close();
        }
    }
}
