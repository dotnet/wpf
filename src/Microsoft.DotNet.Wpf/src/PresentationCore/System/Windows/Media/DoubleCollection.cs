// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MS.Utility;

namespace System.Windows.Media;

public sealed partial class DoubleCollection
{
    /// <summary>
    /// Initializes a new instance using a <paramref name="values"/>. Elements are copied.
    /// </summary>
    /// <param name="values"></param>
    internal DoubleCollection(params ReadOnlySpan<double> values)
    {
        _collection = new FrugalStructList<double>(values.Length);

        foreach (double item in values)
        {
            _collection.Add(item);
        }
    }
}
