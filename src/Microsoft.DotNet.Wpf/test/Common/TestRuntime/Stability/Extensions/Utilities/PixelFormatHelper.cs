// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Reflection;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Utilities
{
    internal static class PixelFormatHelper
    {
        public static bool IsScRGB(PixelFormat format)
        {
            Array formatValues = GetEnumValues(typeof(PixelFormat), "System.Windows.Media.PixelFormatFlags");
            Type formatType = format.GetType();

            PropertyInfo getFlagsProp = formatType.GetProperty("FormatFlags", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);

            if (getFlagsProp == null)
            {
                throw new InvalidOperationException("Could not get the FormatFlags property from the format type " + format.ToString());
            }

            uint flagValue = Convert.ToUInt32(getFlagsProp.GetValue(format, null));

            Enum isScRGB = (Enum)formatValues.GetValue(0);
            foreach (Enum formatFlag in formatValues)
            {
                if (formatFlag.ToString() == "IsScRGB")
                {
                    isScRGB = formatFlag;
                    break;
                }
            }

            return ((flagValue & Convert.ToUInt32(isScRGB)) != 0);
        }

        public static Array GetEnumValues(Type typeInAssembly, string enumName)
        {
            Assembly assembly = Assembly.GetAssembly(typeInAssembly);
            if (assembly == null)
            {
                throw new InvalidOperationException("Could not load assembly for the type " + typeInAssembly.Name);
            }

            Type enumType = assembly.GetType(enumName, false, true);
            if (enumType == null)
            {
                throw new InvalidOperationException("Could not find the " + enumName + " type in " + assembly.FullName);
            }

            return Enum.GetValues(enumType);
        }        
    }
}
