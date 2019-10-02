// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// This interface is used for MarkupExtension's to indicate in the
    /// ProvideValue method the object and property to which this value
    /// will be set. The TargetObject and TargetProperty values may be
    /// null.
    /// </summary>
    [TypeForwardedFrom("PresentationFramework, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public interface IProvideValueTarget
    {
        object TargetObject { get; }
        object TargetProperty { get; }
    }
}
