using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace System.Windows.Controls
{
    public class FallbackBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush;
            }

            if (value is Color)
            {
                return new SolidColorBrush((Color)value);
            }

            // We draw red to visibly see an invalid bind in the UI.
            return new SolidColorBrush(
                new Color
                {
                    A = 255,
                    R = 255,
                    G = 0,
                    B = 0
                }
            );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
