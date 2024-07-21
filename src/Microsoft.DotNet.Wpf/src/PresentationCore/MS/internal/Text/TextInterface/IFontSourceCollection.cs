// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace MS.Internal.Text.TextInterface
{
    internal interface IFontSourceCollection : IEnumerable<IFontSource>
    {
    }

    internal interface IFontSourceCollectionFactory
    {
        IFontSourceCollection Create(string uriString);
    }
}
