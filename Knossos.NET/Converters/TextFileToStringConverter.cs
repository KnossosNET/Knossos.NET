using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Avalonia.Platform;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Knossos.NET.Converters
{
    public class TextFileToStringConverter : IValueConverter
    {
        public static TextFileToStringConverter Instance { get; } = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            try
            {
                if (value == null) return null;

                if (value is not string rawUri || !targetType.IsAssignableFrom(typeof(String)))
                {
                    throw new NotSupportedException();
                }

                Uri uri;

                if (rawUri.StartsWith("avares://"))
                {
                    uri = new Uri(rawUri);
                    var asset = AssetLoader.Open(uri);
                    if (asset != null)
                    {
                        using (var reader = new StreamReader(asset, Encoding.UTF8))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
                else if (rawUri.ToLower().StartsWith("http"))
                {
                    var localPath = Task.Run(() => KnUtils.GetRemoteResource(rawUri)).Result;
                    if (localPath != null)
                    {
                        return File.ReadAllText(localPath);
                    }
                }
                else if (File.Exists(Path.Combine(KnUtils.GetKnossosDataFolderPath(), rawUri)))
                {
                    return File.ReadAllText(Path.Combine(KnUtils.GetKnossosDataFolderPath(), rawUri));
                }
                else if (File.Exists(rawUri))
                {
                    return File.ReadAllText(rawUri);
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
