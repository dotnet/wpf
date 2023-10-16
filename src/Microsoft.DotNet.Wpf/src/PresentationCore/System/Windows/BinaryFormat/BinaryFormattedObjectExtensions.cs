// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace System.Windows
{
    internal static class BinaryFormattedObjectExtensions
    {
        public static bool TryGetFrameworkObject(
            this BinaryFormattedObject format,
            [NotNullWhen(true)] out object? value)
            => format.TryGetPrimitiveType(out value);
        
         public static bool TryGetPrimitiveType(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
         {
            value = default;
            if (format[1] is BinaryObjectString binaryString)
            {
                value = binaryString.Value;
                return true;
            }
            return false;

         }
    }
    
}