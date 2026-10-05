using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FarmaGest.Dominio;
using FarmaGest.Negocio.Servicios;

namespace FarmaGest.UI.Views.Administrador;

/// <summary>Consulta de recetas (solo lectura para el Administrador).</summary>
public partial class RecetasPage : Page
{
    private const string TodasLasObras = "Todas las obras sociales";

    private readonly RecetaService _recetaService;
    private List<RecetaDto> _recetas = new();

    public RecetasPage(RecetaService recetaService)
    {
        InitializeComponent();
        _recetaService = recetaService;
        Loaded += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            _recetas = await _recetaService.ObtenerRecetasAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron cargar las recetas.\n\n{ex.Message}", "FarmaGest",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PendientesText.Text = _recetas.Count(r => r.Estado == Receta.Pendiente).ToString();
        ValidadasMesText.Text = _recetas.Count(r => r.Estado == Receta.Validada).ToString();
        RechazadasMesText.Text = _recetas.Count(r => r.Estado == Receta.Rechazada).ToString();
        UsadasText.Text = _recetas.Count(r => r.VentaId != null).ToString();

        var obras = new List<string> { TodasLasObras };
        obras.AddRange(_recetas.Select(r => r.ObraSocial).Distinct().OrderBy(o => o));
        ObraSocialCombo.ItemsSource = obras;
        ObraSocialCombo.SelectedIndex = 0;

        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string texto = BuscarText.Text.Trim();
        string obra = ObraSocialCombo.SelectedItem as string ?? TodasLasObras;

        string? estado = EstadoCombo.SelectedIndex switch
        {
            1 => Receta.Pendiente,
            2 => Receta.Validada,
            3 => Receta.Rechazada,
            _ => null
        };

        RecetasGrid.ItemsSource = _recetas
            .Where(r => estado == null || r.Estado == estado)
            .Where(r => obra == TodasLasObras || r.ObraSocial == obra)
            .Where(r => texto.Length == 0 ||
                        r.Numero.ToString() == texto ||
                        r.Paciente.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                        r.NumeroAfiliado.ToString() == texto ||
                        r.Medico.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void Filtro_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            AplicarFiltro();
    }

    private void VerDetalle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: RecetaDto r })
            return;

        var texto = new StringBuilder();
        texto.AppendLine($"Receta N° {r.Numero} · {r.Estado}");
        texto.AppendLine($"Afiliado: {r.Paciente} (N° {r.NumeroAfiliado})");
        texto.AppendLine($"Obra social: {r.ObraSocialPlan}");
        texto.AppendLine($"Médico: {r.Medico} ({r.Matricula})");
        texto.AppendLine($"Emisión: {r.FechaEmision:dd/MM/yyyy} · Vence: {r.FechaVencimiento:dd/MM/yyyy}");
        texto.AppendLine();
        texto.AppendLine("Medicamentos:");
        foreach (var m in r.Medicamentos)
            texto.AppendLine($"• {m.Producto} x{m.Cantidad} · cobertura {m.CoberturaTexto}");
        texto.AppendLine();
        if (r.VentaId != null)
            texto.AppendLine($"Usada en la venta N° {r.VentaId}");

        MessageBox.Show(texto.ToString(), "Detalle de la receta", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
