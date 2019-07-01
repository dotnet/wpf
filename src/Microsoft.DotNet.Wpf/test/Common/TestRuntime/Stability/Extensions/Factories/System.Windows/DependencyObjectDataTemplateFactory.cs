// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create object DataTemplate.
    /// Use ValueConverter and MultiValueConverter to display object.
    /// </summary>
    internal class DependencyObjectDataTemplateFactory : DiscoverableFactory<DataTemplate>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an object to set a binding source.
        /// </summary>
        public object BindingSource { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a object DataTemplate which contains a TextBox and a RadioButton in a StackPanel.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataTemplate Create(DeterministicRandom random)
        {
            DataTemplate dataTemplate = new DataTemplate(typeof(DependencyObject));

            FrameworkElementFactory panel = new FrameworkElementFactory(typeof(StackPanel));

            FrameworkElementFactory textBox = new FrameworkElementFactory(typeof(TextBox));
            //Bind object to TextBox content and converter object to string.
            Binding converBinding = new Binding();
            ObjectStringConverter converter = new ObjectStringConverter();
            converBinding.Converter = converter;
            converBinding.ConverterParameter = random.Next();
            converBinding.Mode = BindingMode.OneWay;
            textBox.SetBinding(TextBox.TextProperty, converBinding);

            FrameworkElementFactory button = new FrameworkElementFactory(typeof(RadioButton));
            //Bind two objects to RadioButton content and converter two objects to string.
            MultiBinding multiBinding = new MultiBinding();
            MultiObjectStringConverter multiConverter = new MultiObjectStringConverter();
            Binding bindingAnotherObject = new Binding();
            bindingAnotherObject.Source = BindingSource; 
            multiBinding.Bindings.Add(new Binding());
            multiBinding.Bindings.Add(bindingAnotherObject);
            multiBinding.Mode = BindingMode.OneWay;
            multiBinding.Converter = multiConverter;
            button.SetBinding(RadioButton.ContentProperty, multiBinding);

            panel.AppendChild(textBox);
            panel.AppendChild(button);

            dataTemplate.VisualTree = panel;

            return dataTemplate;
        }

        #endregion
    }

    #region Public Converter

    /// <summary>
    /// A ValueConverter which converter object to string. 
    /// </summary>
    [ValueConversion(typeof(object), typeof(string))]
    public class ObjectStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return value.ToString() + parameter.ToString();
            }
            return parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// A MultiValueConver which converter multiobject to string.
    /// </summary>
    public class MultiObjectStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (object value in values)
            {
                if (value != null)
                {
                    strBuilder.Append(value.ToString());
                }
            }

            return strBuilder.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    #endregion
}
