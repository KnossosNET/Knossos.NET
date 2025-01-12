using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Avalonia.Platform;
using System.Reflection;
using Avalonia.Media.Imaging;
using System.IO;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.Converters
{
    public class BitmapAssetValueConverter : IValueConverter
    {
        public static BitmapAssetValueConverter Instance { get; } = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return null;

                if (value is not string rawUri || !targetType.IsAssignableFrom(typeof(Bitmap)))
                {
                    throw new NotSupportedException();
                }

                Uri uri;

                if (rawUri.StartsWith("avares://"))
                {
                    uri = new Uri(rawUri);
                    var asset = AssetLoader.Open(uri);
                    return new Bitmap(asset);
                }
                else if(rawUri.ToLower().StartsWith("http"))
                {
                    return null;
                }
                else if (File.Exists(Path.Combine(KnUtils.GetKnossosDataFolderPath(), rawUri)))
                {
                    return new Bitmap(Path.Combine(KnUtils.GetKnossosDataFolderPath(), rawUri));
                }
                else if (File.Exists(rawUri))
                {
                    return new Bitmap(rawUri);
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "BitmapAssetValueConverter.Convert()", ex);
            }
            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
