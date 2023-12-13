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
    /// BlurBitmapEffectPrimitive
    /// </summary>
    public sealed partial class BlurBitmapEffect : BitmapEffect
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public BlurBitmapEffect()
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

        /// <summary>
        /// An ImageEffect can be used to emulate a BlurBitmapEffect with certain restrictions. This
        /// method returns true when it is possible to emulate the BlurBitmapEffect using an ImageEffect.
        /// </summary>
        internal override bool CanBeEmulatedUsingEffectPipeline()
        {
            return (Radius <= 100.0);
        }

        /// <summary>
        /// Returns a Effect that emulates this BlurBitmapEffect.
        /// </summary>        
        internal override Effect GetEmulatingEffect()
        {
            if (_imageEffectEmulation != null && _imageEffectEmulation.IsFrozen)
            {
                return _imageEffectEmulation;
            }
            
            if (_imageEffectEmulation == null)
            {
                _imageEffectEmulation = new BlurEffect();
            }

            double radius = Radius;
            if (_imageEffectEmulation.Radius != radius)
            {
                _imageEffectEmulation.Radius = radius;
            }

            KernelType kernelType = KernelType;
            if (_imageEffectEmulation.KernelType != kernelType)
            {
                _imageEffectEmulation.KernelType = kernelType;
            }

            _imageEffectEmulation.RenderingBias = RenderingBias.Performance;

            if (this.IsFrozen)
            {
                _imageEffectEmulation.Freeze();
            }
            
            return _imageEffectEmulation;
        }        

        BlurEffect _imageEffectEmulation;
    }
}
