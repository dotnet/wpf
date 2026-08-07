// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MS.Internal.Text.TextInterface;

namespace MS.Internal.FontCache;

/// <summary>
/// A factory for <see cref="FontSourceCollection"/> implementation.
/// </summary>
internal class FontSourceCollectionFactory : IFontSourceCollectionFactory
{
    public FontSourceCollectionFactory() { }

    public IFontSourceCollection Create(string uriString)
    {
        return new FontSourceCollection(new Uri(uriString));
    }
}
