// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.IO;
namespace System.Windows
{
    /// <summary>
    ///  Writer that writes specific types in binary format without using the BinaryFormatter.
    /// </summary>
    internal static class BinaryFormatWriter
    {

        public static bool TryWriteFrameworkObject(Stream stream, object value)
        {
            
            switch(value)
            {
                case string stringValue:
                    WriteString(stream, stringValue);
                    return true;
                
            }
            return false;
            
        }

        public static void WriteString(Stream stream, string value)
        {
            using var writer = new BinaryFormatWriterScope(stream);
            //new BinaryObjectString(1, value).Write(writer);
            writer.Write(value);
        }

       
        }

        
}