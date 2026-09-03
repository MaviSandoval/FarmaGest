using System;
using System.Windows;
using FarmaGest.Datos.Conexion;
using FarmaGest.Datos.Contexto;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Views.Administrador;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FarmaGest.UI.ViewModels.Compartido;


using FarmaGest.UI.Views.Farmaceutico;
using FarmaGest.UI.ViewModels.Farmaceutico;

namespace FarmaGest.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var servicios = new ServiceCollection();
        ConfigurarServicios(servicios);
        Services = servicios.BuildServiceProvider();

        // ---- Verificación de conexión a la BD ----
        using (var scope = Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FarmaGestDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            bool conecta = await db.Database.CanConnectAsync();

            if (!conecta)
            {
                MessageBox.Show(
                    "No se pudo conectar a la base de datos FarmaGestDB.",
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
        }

        var authWindow = new AuthWindow();
        authWindow.Show();
    }

    private static void ConfigurarServicios(IServiceCollection servicios)
    {
        // ---- Cadena de conexión centralizada (Singleton) ----
        string cadena = ConexionSingleton.Instancia.CadenaConexion;

        // ---- Base de datos (EF Core) ----
        servicios.AddDbContextFactory<FarmaGestDbContext>(opt =>
            opt.UseSqlServer(cadena));

        // ---- Capa de Negocio ----
        servicios.AddSingleton<UsuarioService>();
        servicios.AddSingleton<GestionUsuariosService>();

        // ---- Capa de UI - Administrador ----
        servicios.AddSingleton<MainWindow>();
        servicios.AddTransient<DashboardPage>();
        servicios.AddTransient<VentasPage>();
        servicios.AddTransient<RecetasPage>();
        servicios.AddTransient<ProductosPage>();
        servicios.AddTransient<StockPage>();
        servicios.AddTransient<ClientesPage>();
        servicios.AddTransient<ReportesPage>();
        servicios.AddTransient<ConfiguracionPage>();
        servicios.AddTransient<GestionUsuariosPage>();

        // ---- Capa de UI - Farmacéutico ----
        servicios.AddSingleton<MainWindowFarmaceutico>();
        servicios.AddTransient<DashboardFarmaceuticoPage>();
        servicios.AddTransient<DashboardFarmaceuticoViewModel>();

        // ---- Compartido ----
        servicios.AddTransient<LoginViewModel>();
        servicios.AddTransient<AuthWindow>();
    }
}