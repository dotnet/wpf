// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using SecurityHelper=MS.Internal.SecurityHelper; 

#endregion

namespace System.Windows.Media.Effects
{
    /// <summary>
    /// The class definition for DropShadowBitmapEffect
    /// </summary>
    public partial class DropShadowBitmapEffect
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public DropShadowBitmapEffect()
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
        /// /// Update (propagetes) properties to the unmanaged effect
        /// </summary>                    
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected override void UpdateUnmanagedPropertyState(SafeHandle unmanagedEffect)
        {
        }
        
        /// <summary>
        /// An ImageEffect can be used to emulate a DropShadowBitmapEffect with certain restrictions. This
        /// method returns true when it is possible to emulate the DropShadowBitmapEffect using an ImageEffect.
        /// </summary>
        internal override bool CanBeEmulatedUsingEffectPipeline()
        {
            return (Noise == 0.0);
        }

        /// <summary>
        /// Returns a ImageEffect that emulates this DropShadowBitmapEffect.
        /// </summary>        
        internal override Effect GetEmulatingEffect()
        {
            if (_imageEffectEmulation != null && _imageEffectEmulation.IsFrozen)
            {
                return _imageEffectEmulation;
            }
            
            if (_imageEffectEmulation == null)
            {
                _imageEffectEmulation = new DropShadowEffect();
            }

            Color color = Color;
            if (_imageEffectEmulation.Color != color)
            {
                _imageEffectEmulation.Color = color;
            }
            // The limits on ShadowDepth preserve existing behavior.
            // A scale transform can scale the shadow depth to exceed 50.0, 
            // and this behavior is also handled correctly in the unmanaged layer.
            double shadowDepth = ShadowDepth;
            if (_imageEffectEmulation.ShadowDepth != shadowDepth)
            {
                if (shadowDepth >= 50.0)
                {
                    _imageEffectEmulation.ShadowDepth = 50.0;
                }
                else if (shadowDepth < 0.0)
                {
                    _imageEffectEmulation.ShadowDepth = 0.0;
                }
                else
                {
                    _imageEffectEmulation.ShadowDepth = shadowDepth;
                }
            }

            double direction = Direction;
            if (_imageEffectEmulation.Direction != direction)
            {
                _imageEffectEmulation.Direction = direction;
            }

            double opacity = Opacity;
            if (_imageEffectEmulation.Opacity != opacity)
            {
                if (opacity >= 1.0)
                {
                    _imageEffectEmulation.Opacity = 1.0;
                }
                else if (opacity <= 0.0)
                {
                    _imageEffectEmulation.Opacity = 0.0;
                }
                else
                {
                    _imageEffectEmulation.Opacity = opacity;
                }
            }

            double softness = Softness;
            if (_imageEffectEmulation.BlurRadius / _MAX_EMULATED_BLUR_RADIUS != softness)
            {
                if (softness >= 1.0)
                {
                    _imageEffectEmulation.BlurRadius = _MAX_EMULATED_BLUR_RADIUS;
                }
                else if (softness <= 0.0)
                {
                    _imageEffectEmulation.BlurRadius = 0.0;
                }
                else
                {
                    _imageEffectEmulation.BlurRadius = _MAX_EMULATED_BLUR_RADIUS * softness;
                }
            }

            _imageEffectEmulation.RenderingBias = RenderingBias.Performance;

            if (this.IsFrozen)
            {
                _imageEffectEmulation.Freeze();
            }

            return _imageEffectEmulation;
        }        

        DropShadowEffect _imageEffectEmulation;

        private const double _MAX_EMULATED_BLUR_RADIUS = 25.0;
    }
}
