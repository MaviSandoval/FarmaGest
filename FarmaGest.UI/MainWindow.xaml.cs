using System;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.UI.Views.Administrador;
using Microsoft.Extensions.DependencyInjection;
using FarmaGest.Negocio.Servicios;
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

        RootFrame.Navigate(App.Services.GetRequiredService<DashboardPage>());
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

    private void CerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        _sesionUsuarioService.CerrarSesion();

        var authWindow =
            App.Services.GetRequiredService<AuthWindow>();

        authWindow.Show();

        Close();
    }
}
