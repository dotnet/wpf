// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    public class ConverterForBinding : IValueConverter
    {
        #region Public Members

        public int ConverterInt
        {
            get { return converterInt; }
            set { converterInt = value; }
        }

        public string ConverterString
        {
            get { return converterString; }
            set { converterString = value; }
        }

        public bool ConverterBool
        {
            get { return converterBool; }
            set { converterBool = value; }
        }

        public ConverterForBinding() { }

        public ConverterForBinding(int randomInt, string randomString, bool randomBool)
        {
            ConverterInt = randomInt;
            ConverterString = randomString;
            ConverterBool = randomBool;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(int))
            {
                return (int)(System.Convert.ToInt32(value) / ConverterInt);
            }
            if (targetType == typeof(float))
            {
                return (float)(System.Convert.ToSingle(value) / ConverterInt);
            }
            if (targetType == typeof(double))
            {
                return (double)(System.Convert.ToDouble(value) / ConverterInt);
            }
            if (targetType == typeof(bool))
            {
                return System.Convert.ToBoolean(value);
            }
            if (targetType == typeof(string))
            {
                return System.Convert.ToString(value);
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(int))
            {
                return (int)(System.Convert.ToInt32(value) / ConverterInt);
            }
            if (targetType == typeof(float))
            {
                return (float)(System.Convert.ToSingle(value) / ConverterInt);
            }
            if (targetType == typeof(double))
            {
                return (double)(System.Convert.ToDouble(value) / ConverterInt);
            }
            if (targetType == typeof(bool))
            {
                return System.Convert.ToBoolean(value);
            }
            if (targetType == typeof(string))
            {
                return System.Convert.ToString(value);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Private Data

        private int converterInt;
        private string converterString;
        private bool converterBool;

        #endregion
    }
}
