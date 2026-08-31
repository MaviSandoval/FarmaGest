using System;
using System.Windows;
using FarmaGest.Datos.Contexto;
using FarmaGest.Negocio.Servicios;
using FarmaGest.UI.Views.Administrador;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FarmaGest.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var servicios = new ServiceCollection();
        ConfigurarServicios(servicios);
        Services = servicios.BuildServiceProvider();

        var authWindow = new AuthWindow();
        authWindow.Show();
    }

    private static void ConfigurarServicios(IServiceCollection servicios)
    {
        // ---- Base de datos (EF Core) ----
        servicios.AddDbContext<FarmaGestDbContext>(opt =>
            opt.UseSqlServer(
                "Server=MAVI\\SQLEXPRESS;Database=FarmaGestDB;Trusted_Connection=True;TrustServerCertificate=True;"));

        // ---- Capa de Negocio ----
        // Por ahora usamos la implementación en memoria. Cuando FarmaGest.Datos
        // tenga los repositorios reales, solo se cambia esta línea.
        servicios.AddSingleton<IDashboardService, DashboardServiceEnMemoria>();

        // ---- Capa de UI ----
        servicios.AddSingleton<MainWindow>();
        servicios.AddTransient<DashboardPage>();
        servicios.AddTransient<VentasPage>();
        servicios.AddTransient<RecetasPage>();
        servicios.AddTransient<ProductosPage>();
        servicios.AddTransient<StockPage>();
        servicios.AddTransient<ProveedoresPage>();
        servicios.AddTransient<ClientesPage>();
        servicios.AddTransient<ReportesPage>();
        servicios.AddTransient<ConfiguracionPage>();
        servicios.AddScoped<GestionUsuariosService>();
        servicios.AddTransient<GestionUsuariosPage>();
    }
}