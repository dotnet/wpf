// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Xaml
{
    public interface IXamlLineInfo
    {
        bool HasLineInfo { get; }

        int LineNumber { get; }
        int LinePosition { get; }
    }
}
