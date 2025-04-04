// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#region Using directives

using System.Runtime.InteropServices;

#endregion

namespace System.Windows.Media.Effects
{
    /// <summary>
    /// The class definition for EmbossBitmapEffect
    /// </summary>
    public partial class EmbossBitmapEffect
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public EmbossBitmapEffect()
        {
        }

        /// <summary>
        /// Creates the unmanaged effect handle
        /// </summary>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected override unsafe SafeHandle CreateUnmanagedEffect()
        {
            return null;
        }

        /// <summary>
        /// /// Update (propagetes) properties to the unmanaged effect
        /// </summary>                    
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected override void UpdateUnmanagedPropertyState(SafeHandle unmanagedEffect)
        {
        }
    }
}
