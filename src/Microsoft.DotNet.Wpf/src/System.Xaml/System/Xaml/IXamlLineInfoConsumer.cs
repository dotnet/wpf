// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace System.Xaml
{
    public interface IXamlLineInfoConsumer
    {
        void SetLineInfo(int lineNumber, int linePosition);
        bool ShouldProvideLineInfo { get; }
    }
}
