using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace FarmaGest.UI.Helpers;

/// <summary>
/// Conversión de imágenes para la foto de perfil.
/// La foto se reduce a 256 px de ancho y se guarda como JPG en la base.
/// </summary>
public static class ImagenHelper
{
    private const int AnchoMaximo = 256;

    /// <summary>Lee una imagen del disco, la reduce y la devuelve como JPG en bytes.</summary>
    public static byte[] CargarYReducir(string rutaArchivo)
    {
        var imagen = new BitmapImage();
        imagen.BeginInit();
        imagen.CacheOption = BitmapCacheOption.OnLoad;
        imagen.UriSource = new Uri(rutaArchivo);
        imagen.DecodePixelWidth = AnchoMaximo;
        imagen.EndInit();
        imagen.Freeze();

        var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
        encoder.Frames.Add(BitmapFrame.Create(imagen));

        using var memoria = new MemoryStream();
        encoder.Save(memoria);
        return memoria.ToArray();
    }

    /// <summary>Convierte los bytes guardados en la base en una imagen para mostrar.</summary>
    public static BitmapImage? DesdeBytes(byte[]? datos)
    {
        if (datos == null || datos.Length == 0)
            return null;

        using var memoria = new MemoryStream(datos);

        var imagen = new BitmapImage();
        imagen.BeginInit();
        imagen.CacheOption = BitmapCacheOption.OnLoad;
        imagen.StreamSource = memoria;
        imagen.EndInit();
        imagen.Freeze();

        return imagen;
    }
}
