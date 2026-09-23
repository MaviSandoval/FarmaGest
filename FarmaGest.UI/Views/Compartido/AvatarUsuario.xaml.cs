using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FarmaGest.Dominio;
using FarmaGest.UI.Helpers;

namespace FarmaGest.UI.Views.Compartido;

public partial class AvatarUsuario : UserControl
{
    public AvatarUsuario()
    {
        InitializeComponent();
    }

    public void Mostrar(Usuario? usuario)
    {
        if (usuario == null)
        {
            InicialesText.Text = string.Empty;
            FotoEllipse.Visibility = Visibility.Collapsed;
            return;
        }

        // Iniciales (ej.: "Lucía Sánchez" -> "LS")
        string inicialNombre = usuario.Nombre.Length > 0 ? usuario.Nombre[..1] : string.Empty;
        string inicialApellido = usuario.Apellido.Length > 0 ? usuario.Apellido[..1] : string.Empty;
        InicialesText.Text = (inicialNombre + inicialApellido).ToUpper();
        InicialesText.FontSize = Height * 0.38;

        // Foto, si tiene
        var foto = ImagenHelper.DesdeBytes(usuario.Foto);

        if (foto != null)
        {
            FotoEllipse.Fill = new ImageBrush(foto) { Stretch = Stretch.UniformToFill };
            FotoEllipse.Visibility = Visibility.Visible;
        }
        else
        {
            FotoEllipse.Visibility = Visibility.Collapsed;
        }
    }
}
