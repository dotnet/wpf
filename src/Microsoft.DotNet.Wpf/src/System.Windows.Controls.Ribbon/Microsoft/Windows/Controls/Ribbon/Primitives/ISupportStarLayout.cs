// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    /// <summary>
    ///     Interface for the element which computes and allocates the
    ///     widths in the star layout contract.
    /// </summary>
    public interface ISupportStarLayout
    {
        /// <summary>
        ///     The method through which a provider (IProvideStarLayout) element
        ///     register itself to participate in the star layout.
        /// </summary>
        void RegisterStarLayoutProvider(IProvideStarLayoutInfoBase starLayoutInfoProvider);

        /// <summary>
        ///     The method through which a provider (IProvideStarLayout) element
        ///     unregisters itself from star layout participation.
        /// </summary>
        void UnregisterStarLayoutProvider(IProvideStarLayoutInfoBase starLayoutInfoProvider);

        /// <summary>
        ///     The property which lets proviers know whether the current layout (measure) pass
        ///     is after star allocation or not.
        /// </summary>
        bool IsStarLayoutPass { get; }
    }
}