// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using MS.Internal;
using System.Windows.Media.Animation;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Windows.Media.Composition;
using System.Diagnostics;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;


namespace System.Windows.Media.Effects
{
    /// <summary>
    /// The class defintion for BitmapEffectGroup
    /// </summary>
    [ContentProperty("Children")]
    public sealed partial class BitmapEffectGroup : BitmapEffect
    {
        /// <summary>
        /// 
        /// </summary>
        public BitmapEffectGroup()
        {
        }
        
        /// <summary>
        /// 1. Updates (propagates) the properties to the unmanaged handle of all
        /// the child effects
        /// 2. Sets up all the connections
        /// 3. Wraps the list with the aggregate effect
        /// </summary>
        /// <param name="unmanagedEffect">Unmanaged handle for aggregate effect</param>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected override void UpdateUnmanagedPropertyState(SafeHandle unmanagedEffect)
        {
        }
        
        /// <summary>
        /// Create an unmanaged handle for the group effect
        /// </summary>
        /// <returns></returns>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected override unsafe SafeHandle CreateUnmanagedEffect()
        {
            return null;
        }
    }
}
