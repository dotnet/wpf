// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

using System.Runtime.InteropServices;


namespace System.Windows.Media.Effects
{
    /// <summary>
    /// BevelBitmapEffectPrimitive
    /// </summary>
    public sealed partial class BevelBitmapEffect : BitmapEffect
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public BevelBitmapEffect()
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
        /// Update (propagetes) properties to the unmanaged effect
        /// </summary>    
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected override void UpdateUnmanagedPropertyState(SafeHandle unmanagedEffect)
        {
        }
    }
}
