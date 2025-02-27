// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Windows;

public sealed partial class DataObject
{
    private partial class DataStore
    {
        private class DataStoreEntry
        {
            public DataStoreEntry(object? data, bool autoConvert)
            {
                Data = data;
                AutoConvert = autoConvert;
            }

            // Data object property.
            public object? Data { get; set; }

            // Auto convert proeprty.
            public bool AutoConvert { get; }
        }
    }
}
