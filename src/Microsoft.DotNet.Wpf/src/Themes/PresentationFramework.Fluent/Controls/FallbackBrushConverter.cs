// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace Fluent.Controls
{
    internal class FallbackBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush;
            }

            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }

            // We draw red to visibly see an invalid bind in the UI.
            return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
