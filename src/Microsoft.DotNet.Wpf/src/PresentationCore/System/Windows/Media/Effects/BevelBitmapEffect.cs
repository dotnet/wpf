// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using MS.Internal;
using System;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using MS.Internal.PresentationCore;
using System.Security;


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
        unsafe protected override SafeHandle CreateUnmanagedEffect()
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
