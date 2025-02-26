// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Runtime.InteropServices.ComTypes;

namespace System.Windows;

public sealed partial class DataObject
{
    private partial class DataStore
    {
        private class DataStoreEntry
        {
            public DataStoreEntry(object? data, bool autoConvert, DVASPECT aspect, int index)
            {
                Data = data;
                AutoConvert = autoConvert;
                Aspect = aspect;
                Index = index;
            }

            // Data object property.
            public object? Data { get; set; }

            // Auto convert proeprty.
            public bool AutoConvert { get; }

            // Aspect flag property.
            public DVASPECT Aspect { get; }

            // Index property.
            public int Index { get; }
        }
    }
}
