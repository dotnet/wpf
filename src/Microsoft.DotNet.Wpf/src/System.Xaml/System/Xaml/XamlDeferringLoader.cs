// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Xaml
{
    // Implemented by templates and any other processors of deferred content.
    public abstract class XamlDeferringLoader
    {
        public abstract object Load(XamlReader xamlReader, IServiceProvider serviceProvider);

        public abstract XamlReader Save(object value, IServiceProvider serviceProvider);
    }
}
