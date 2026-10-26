using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly DispatcherTimer _notificacionesTimer;
        private readonly NotificacionService _notificacionService;

        public MainWindowFarmaceutico(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _sesionUsuarioService = _serviceProvider.GetRequiredService<SesionUsuarioService>();
            _notificacionService = _serviceProvider.GetRequiredService<NotificacionService>();

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

            // ---- Notificaciones: al abrir y cada 2 minutos ----
            _notificacionesTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            _notificacionesTimer.Tick += async (_, _) => await CargarNotificacionesAsync();
            _notificacionesTimer.Start();
            Loaded += async (_, _) => await CargarNotificacionesAsync();

            Closed += (_, _) =>
            {
                _relojTimer.Stop();
                _notificacionesTimer.Stop();
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
                    RootFrame.Navigate(_serviceProvider.GetRequiredService<ValidacionRecetasPage>());
                    break;

                case nameof(NavVentas):
                    RootFrame.Navigate(_serviceProvider.GetRequiredService<VentasFarmaceuticoPage>());
                    break;

                case nameof(NavProductos):
                    RootFrame.Navigate(_serviceProvider.GetRequiredService<ProductosFarmaceuticoPage>());
                    break;

                case nameof(NavStock):
                    RootFrame.Navigate(_serviceProvider.GetRequiredService<StockFarmaceuticoPage>());
                    break;

                case nameof(NavObrasSociales):
                    RootFrame.Navigate(_serviceProvider.GetRequiredService<ObrasSocialesPage>());
                    break;

                case nameof(NavReportes):
                    RootFrame.Navigate(_serviceProvider.GetRequiredService<ReportesFarmaceuticoPage>());
                    break;
            }
        }

        // =========================================================
        // NOTIFICACIONES (stock y recetas)
        // =========================================================
        private async Task CargarNotificacionesAsync()
        {
            try
            {
                var avisos = await _notificacionService.ObtenerAsync();

                NotificacionesList.ItemsSource = avisos;
                SinNotificacionesText.Visibility = avisos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                NotificacionesResumenText.Text = $"Actualizado {DateTime.Now:HH:mm}";

                NotificacionesBadge.Visibility = avisos.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                NotificacionesCantidadText.Text = avisos.Count > 9 ? "9+" : avisos.Count.ToString();
            }
            catch
            {
                // Las notificaciones son un extra: si falla la consulta no se interrumpe el trabajo
                NotificacionesResumenText.Text = "No se pudieron actualizar";
            }
        }

        private async void Notificaciones_Click(object sender, RoutedEventArgs e)
        {
            await CargarNotificacionesAsync();
            NotificacionesPopup.IsOpen = true;
        }

        /// <summary>Lleva a la pantalla donde se resuelve el aviso.</summary>
        private void Notificacion_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: NotificacionDto aviso })
                return;

            NotificacionesPopup.IsOpen = false;

            if (aviso.Destino == NotificacionService.DestinoStock)
            {
                NavStock.IsChecked = true;
                RootFrame.Navigate(_serviceProvider.GetRequiredService<StockFarmaceuticoPage>());
            }
            else
            {
                NavRecetas.IsChecked = true;
                RootFrame.Navigate(_serviceProvider.GetRequiredService<ValidacionRecetasPage>());
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
