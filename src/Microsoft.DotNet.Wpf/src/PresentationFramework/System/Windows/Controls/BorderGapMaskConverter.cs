// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Used to create a Gap in the Border for GroupBox style
//

using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace System.Windows.Controls
{
    /// <summary>
    /// BorderGapMaskConverter class
    /// </summary>
    public class BorderGapMaskConverter : IMultiValueConverter
    {
        /// <summary>
        /// Convert a value.
        /// </summary>
        /// <param name="values">values as produced by source binding</param>
        /// <param name="targetType">target type</param>
        /// <param name="parameter">converter parameter</param>
        /// <param name="culture">culture information</param>
        /// <returns>
        /// Converted value.
        /// Visual Brush that is used as the opacity mask for the Border
        /// in the style for GroupBox.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //
            // Parameter Validation
            //

            Type doubleType = typeof(double);

            if (parameter == null ||
                values == null ||
                values.Length != 3 ||
                values[0] == null ||
                values[1] == null ||
                values[2] == null ||
                !doubleType.IsAssignableFrom(values[0].GetType()) ||
                !doubleType.IsAssignableFrom(values[1].GetType()) ||
                !doubleType.IsAssignableFrom(values[2].GetType()) )
            {
                return DependencyProperty.UnsetValue;
            }

            Type paramType = parameter.GetType();
            if (!(doubleType.IsAssignableFrom(paramType) || typeof(string).IsAssignableFrom(paramType)))
            {
                return DependencyProperty.UnsetValue;
            }

            //
            // Conversion
            //

            double headerWidth = (double)values[0];
            double borderWidth = (double)values[1];
            double borderHeight = (double)values[2];

            // Doesn't make sense to have a Grid
            // with 0 as width or height
            if (borderWidth == 0
                || borderHeight == 0)
            {
                return null;
            }

            // Width of the line to the left of the header
            // to be used to set the width of the first column of the Grid
            double lineWidth;
            if (parameter is string)
            {
                lineWidth = Double.Parse(((string)parameter), NumberFormatInfo.InvariantInfo);
            }
            else
            {
                lineWidth = (double)parameter;
            }

            Grid grid = new Grid();
            grid.Width = borderWidth;
            grid.Height = borderHeight;
            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            ColumnDefinition colDef3 = new ColumnDefinition();
            colDef1.Width = new GridLength(lineWidth);
            colDef2.Width = new GridLength(headerWidth);
            colDef3.Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions.Add(colDef1);
            grid.ColumnDefinitions.Add(colDef2);
            grid.ColumnDefinitions.Add(colDef3);
            RowDefinition rowDef1 = new RowDefinition();
            RowDefinition rowDef2 = new RowDefinition();
            rowDef1.Height = new GridLength(borderHeight / 2);
            rowDef2.Height = new GridLength(1, GridUnitType.Star);
            grid.RowDefinitions.Add(rowDef1);
            grid.RowDefinitions.Add(rowDef2);

            Rectangle rectColumn1 = new Rectangle();
            Rectangle rectColumn2 = new Rectangle();
            Rectangle rectColumn3 = new Rectangle();
            rectColumn1.Fill = Brushes.Black;
            rectColumn2.Fill = Brushes.Black;
            rectColumn3.Fill = Brushes.Black;

            Grid.SetRowSpan(rectColumn1, 2);
            Grid.SetRow(rectColumn1, 0);
            Grid.SetColumn(rectColumn1, 0);

            Grid.SetRow(rectColumn2, 1);
            Grid.SetColumn(rectColumn2, 1);

            Grid.SetRowSpan(rectColumn3, 2);
            Grid.SetRow(rectColumn3, 0);
            Grid.SetColumn(rectColumn3, 2);

            grid.Children.Add(rectColumn1);
            grid.Children.Add(rectColumn2);
            grid.Children.Add(rectColumn3);

            return (new VisualBrush(grid));
        }

        /// <summary>
        /// Not Supported
        /// </summary>
        /// <param name="value">value, as produced by target</param>
        /// <param name="targetTypes">target types</param>
        /// <param name="parameter">converter parameter</param>
        /// <param name="culture">culture information</param>
        /// <returns>Nothing</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing };
        }
    }
}
