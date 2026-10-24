using System;
using System.Windows;
using FarmaGest.Datos.Conexion;
using FarmaGest.Datos.Contexto;
using FarmaGest.Datos.Repositorios;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Views.Administrador;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FarmaGest.UI.ViewModels.Compartido;

using FarmaGest.UI.Views.Farmaceutico;
using FarmaGest.UI.ViewModels.Farmaceutico;
using FarmaGest.UI.Views.Cajero;
using FarmaGest.UI.Views.Compartido;

namespace FarmaGest.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Color de acento de la marca (verde FarmaGest)
        Wpf.Ui.Appearance.ApplicationAccentColorManager.Apply(
            System.Windows.Media.Color.FromRgb(0x1F, 0x7A, 0x6C),
            Wpf.Ui.Appearance.ApplicationTheme.Light);

        var servicios = new ServiceCollection();
        ConfigurarServicios(servicios);
        Services = servicios.BuildServiceProvider();

        // ---- Verificación de conexión a la BD ----
        using (var scope = Services.CreateScope())
        {
            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<FarmaGestDbContext>>();

            await using var db = await factory.CreateDbContextAsync();

            bool conecta = await db.Database.CanConnectAsync();

            if (!conecta)
            {
                MessageBox.Show(
                    "No se pudo conectar a la base de datos FarmaGestDB.",
                    "Error de conexión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
                return;
            }
        }

        // ---- Copia de seguridad automática diaria (00:00) ----
        Services.GetRequiredService<BackupProgramadoService>().Iniciar();

        var authWindow = new AuthWindow();
        authWindow.Show();
    }

    private static void ConfigurarServicios(IServiceCollection servicios)
    {
        // ---- Cadena de conexión centralizada ----
        string cadena = ConexionSingleton.Instancia.CadenaConexion;

        // ---- Base de datos ----
        servicios.AddDbContextFactory<FarmaGestDbContext>(opt =>
            opt.UseSqlServer(cadena));

        // =====================================================
        // CAPA DE DATOS
        // =====================================================

        servicios.AddSingleton<UsuarioRepositorio>();
        servicios.AddSingleton<BackupRepositorio>();

        // =====================================================
        // CAPA DE NEGOCIO
        // =====================================================

        servicios.AddSingleton<UsuarioService>();
        servicios.AddSingleton<GestionUsuariosService>();
        servicios.AddSingleton<IDashboardService, DashboardServiceEnMemoria>();
        servicios.AddSingleton<ProductoService>();
        servicios.AddSingleton<SesionUsuarioService>();
        servicios.AddSingleton<CajaService>();
        servicios.AddSingleton<FacturacionService>();

        // Servicios agregados por los cambios nuevos
        servicios.AddSingleton<StockService>();
        servicios.AddSingleton<RecetaService>();
        servicios.AddSingleton<VentaService>();
        servicios.AddSingleton<ObraSocialService>();
        servicios.AddSingleton<ReporteService>();
        servicios.AddSingleton<BackupService>();
        servicios.AddSingleton<BackupProgramadoService>();

        // =====================================================
        // UI - ADMINISTRADOR
        // =====================================================

        servicios.AddTransient<MainWindow>();
        servicios.AddTransient<DashboardPage>();
        servicios.AddTransient<VentasPage>();
        servicios.AddTransient<RecetasPage>();
        
        servicios.AddTransient<StockPage>();
        servicios.AddTransient<ClientesPage>();
        servicios.AddTransient<ReportesPage>();
        servicios.AddTransient<ConfiguracionPage>();
        servicios.AddTransient<GestionUsuariosPage>();

        // =====================================================
        // UI - FARMACÉUTICO
        // =====================================================

        servicios.AddTransient<MainWindowFarmaceutico>();
        servicios.AddTransient<DashboardFarmaceuticoPage>();
        servicios.AddTransient<DashboardFarmaceuticoViewModel>();

        servicios.AddTransient<ValidacionRecetasPage>();
        servicios.AddTransient<NuevaVentaPage>();
        servicios.AddTransient<VentasFarmaceuticoPage>();
        servicios.AddTransient<ProductosFarmaceuticoPage>();
        servicios.AddTransient<StockFarmaceuticoPage>();
        servicios.AddTransient<ObrasSocialesPage>();
        servicios.AddTransient<ReportesFarmaceuticoPage>();

        // =====================================================
        // UI - CAJERO
        // =====================================================

        servicios.AddTransient<MainWindowCajero>();

        // Inicio del cajero conectado a datos reales
        servicios.AddTransient<DashboardCajeroPage>();

        servicios.AddTransient<CajaPage>();
        servicios.AddTransient<PreventasPage>();
        servicios.AddTransient<VentasCobradasPage>();

        // Resumen de caja conectado a datos reales
        servicios.AddTransient<ResumenCajaPage>();

        // =====================================================
        // UI - COMPARTIDO
        // =====================================================

        servicios.AddTransient<LoginViewModel>();
        servicios.AddTransient<AuthWindow>();
        servicios.AddTransient<MiPerfilPage>();
        servicios.AddTransient<CambiarContrasenaViewModel>();
    }
}