// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    /// <summary>
    ///     The interface is the star layout contract which provides
    ///     the data needed to do the star layout.
    /// </summary>
    public interface IProvideStarLayoutInfoBase
    {
        /// <summary>
        ///     The callback method which gets called every time before
        ///     the star allocator (ISupportStarLayout) gets measured
        /// </summary>
        void OnInitializeLayout();

        /// <summary>
        ///     The UIElement which this provider targets.
        /// </summary>
        UIElement TargetElement { get; }
    }
}