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

            // ---- Vista inicial: Inicio del Cajero ----
            RootFrame.Navigate(new DashboardCajeroPage());

            // ---- Reloj de la barra superior ----
            _relojTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _relojTimer.Tick += (_, _) => ActualizarFechaHora();
            _relojTimer.Start();

            ActualizarFechaHora();

            // ---- Encabezado con el nombre y la foto del usuario logueado ----
            ActualizarEncabezado();
            _sesionUsuarioService.UsuarioActualizado += ActualizarEncabezado;

            Closed += (_, _) =>
            {
                _relojTimer.Stop();
                _sesionUsuarioService.UsuarioActualizado -= ActualizarEncabezado;
            };
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

        private void MiPerfil_Click(object sender, RoutedEventArgs e)
        {
            // Ninguna opción del menú queda marcada mientras se está en Mi perfil
            foreach (var item in NavPanel.Children.OfType<RadioButton>())
                item.IsChecked = false;

            RootFrame.Navigate(_serviceProvider.GetRequiredService<MiPerfilPage>());
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
                    RootFrame.Navigate(new DashboardCajeroPage());
                    break;

                case nameof(NavPreventas):
                    // MAQUETA para la presentación. Para volver a la pantalla real (con la base de datos),
                    // reemplazar por: RootFrame.Navigate(_serviceProvider.GetRequiredService<PreventasPage>());
                    RootFrame.Navigate(new PreventasMaquetaPage());
                    break;

                case nameof(NavVentasCobradas):
                    // MAQUETA para la presentación. Para volver a la pantalla real (con la base de datos),
                    // reemplazar por: RootFrame.Navigate(_serviceProvider.GetRequiredService<VentasCobradasPage>());
                    RootFrame.Navigate(new VentasCobradasMaquetaPage());
                    break;

                case nameof(NavCaja):
                    RootFrame.Navigate(
                        _serviceProvider.GetRequiredService<CajaPage>());
                    break;

                case nameof(NavResumen):
                    RootFrame.Navigate(new ResumenCajaPage());
                    break;
            }
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

            var authWindow =
                _serviceProvider.GetRequiredService<AuthWindow>();

            authWindow.Show();
            Close();
        }
    }
}