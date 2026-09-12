using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageReorder.Services;

internal static class ThumbnailLoader
{
    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jfif", ".png", ".gif", ".bmp", ".tif", ".tiff"
    };

    public static ImageSource? TryLoad(string path, int decodePixelWidth)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            image.DecodePixelWidth = decodePixelWidth;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Could not decode a thumbnail for {path}.", ex);
            return null;
        }
    }

    public static bool IsSupportedImage(string path)
        => SupportedExtensions.Contains(Path.GetExtension(path));
}
