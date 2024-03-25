using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Knossos.NET.Converters
{
    public class EscapeUnderscoresConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            switch (value)
            {
                case string sourceText:
                    return KnUtils.EscapeUnderscores(sourceText);
            }
            // unknown type (uncomment Console line to get object type and add to switch)
            // Console.WriteLine(value?.GetType());
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}