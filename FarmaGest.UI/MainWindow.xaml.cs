using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Views.Administrador;
using FarmaGest.UI.Views.Compartido;
using Microsoft.Extensions.DependencyInjection;

namespace FarmaGest.UI;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly SesionUsuarioService _sesionUsuarioService;

    public MainWindow(SesionUsuarioService sesionUsuarioService)
    {
        InitializeComponent();

        _sesionUsuarioService = sesionUsuarioService;

        FechaHoraText.Text = DateTime.Now.ToString(
            "dddd, d 'de' MMMM 'de' yyyy - HH:mm",
            new System.Globalization.CultureInfo("es-AR"));

        // Encabezado con el nombre y la foto del usuario logueado
        ActualizarEncabezado();
        _sesionUsuarioService.UsuarioActualizado += ActualizarEncabezado;
        Closed += (_, _) => _sesionUsuarioService.UsuarioActualizado -= ActualizarEncabezado;

        RootFrame.Navigate(App.Services.GetRequiredService<DashboardPage>());
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
        if (sender is not RadioButton rb) return;

        Page? page = rb.Name switch
        {
            nameof(NavInicio) => App.Services.GetRequiredService<DashboardPage>(),
            nameof(NavVentas) => App.Services.GetRequiredService<VentasPage>(),
            nameof(NavRecetas) => App.Services.GetRequiredService<RecetasPage>(),
            nameof(NavProductos) => App.Services.GetRequiredService<ProductosPage>(),
            nameof(NavStock) => App.Services.GetRequiredService<StockPage>(),
            nameof(NavReportes) => App.Services.GetRequiredService<ReportesPage>(),
            nameof(NavConfiguracion) => App.Services.GetRequiredService<ConfiguracionPage>(),
            nameof(NavUsuarios) => App.Services.GetRequiredService<GestionUsuariosPage>(),
            _ => null
        };

        if (page is not null)
            RootFrame.Navigate(page);
    }

    private void MiPerfil_Click(object sender, RoutedEventArgs e)
    {
        // Ninguna opción del menú queda marcada mientras se está en Mi perfil
        foreach (var item in NavPanel.Children.OfType<RadioButton>())
            item.IsChecked = false;

        RootFrame.Navigate(App.Services.GetRequiredService<MiPerfilPage>());
    }

    private void CerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        var respuesta = MessageBox.Show(
            "¿Querés cerrar la sesión?",
            "Cerrar sesión",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (respuesta != MessageBoxResult.Yes)
            return;

        _sesionUsuarioService.CerrarSesion();

        var authWindow = App.Services.GetRequiredService<AuthWindow>();
        authWindow.Show();

        Close();
    }
}
