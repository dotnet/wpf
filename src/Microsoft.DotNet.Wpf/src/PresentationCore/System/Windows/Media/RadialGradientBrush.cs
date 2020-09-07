// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: This file contains the implementation of RadialGradientBrush.
//              The RadialGradientBrush is a GradientBrush which defines its
//              Gradient as a radial interpolation within an Ellipse.
//
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// RadialGradientBrush - This GradientBrush defines its Gradient as an interpolation
    /// within an Ellipse.
    /// </summary>
    public sealed partial class RadialGradientBrush : GradientBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for RadialGradientBrush.  The resulting brush has no content.
        /// </summary>
        public RadialGradientBrush() : base()
        {
        }

        /// <summary>
        /// RadialGradientBrush Constructor
        /// Constructs a RadialGradientBrush with two colors specified for GradientStops at
        /// offsets 0.0 and 1.0.
        /// </summary>
        /// <param name="startColor"> The Color at offset 0.0. </param>
        /// <param name="endColor"> The Color at offset 1.0. </param>        
        public RadialGradientBrush(Color startColor,
                                   Color endColor) : base()
        {
            GradientStops.Add(new GradientStop(startColor, 0.0));
            GradientStops.Add(new GradientStop(endColor, 1.0));
        }

        /// <summary>
        /// RadialGradientBrush Constructor
        /// Constructs a RadialGradientBrush with GradientStops set to the passed-in 
        /// collection.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        public RadialGradientBrush(GradientStopCollection gradientStopCollection) 
                                                   : base(gradientStopCollection)
        {            
        }
            

        #endregion Constructors

        private void ManualUpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                Transform vTransform = Transform;
                Transform vRelativeTransform = RelativeTransform;
                GradientStopCollection vGradientStops = GradientStops;

                DUCE.ResourceHandle hTransform;
                if (vTransform == null ||
                    Object.ReferenceEquals(vTransform, Transform.Identity)
                    )
                {
                    hTransform = DUCE.ResourceHandle.Null;                                        
                }
                else
                {
                    hTransform = ((DUCE.IResource)vTransform).GetHandle(channel);
                }
                DUCE.ResourceHandle hRelativeTransform;
                if (vRelativeTransform == null ||
                    Object.ReferenceEquals(vRelativeTransform, Transform.Identity)
                    )
                {
                    hRelativeTransform = DUCE.ResourceHandle.Null;                                        
                }
                else
                {
                    hRelativeTransform = ((DUCE.IResource)vRelativeTransform).GetHandle(channel);
                }
                DUCE.ResourceHandle hOpacityAnimations = GetAnimationResourceHandle(OpacityProperty, channel);
                DUCE.ResourceHandle hCenterAnimations = GetAnimationResourceHandle(CenterProperty, channel);
                DUCE.ResourceHandle hRadiusXAnimations = GetAnimationResourceHandle(RadiusXProperty, channel);
                DUCE.ResourceHandle hRadiusYAnimations = GetAnimationResourceHandle(RadiusYProperty, channel);
                DUCE.ResourceHandle hGradientOriginAnimations = GetAnimationResourceHandle(GradientOriginProperty, channel);

                DUCE.MILCMD_RADIALGRADIENTBRUSH data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdRadialGradientBrush;
                    data.Handle = _duceResource.GetHandle(channel);
                    double tempOpacity = Opacity;
                    DUCE.CopyBytes((byte*)&data.Opacity, (byte*)&tempOpacity, 8);
                    data.hOpacityAnimations = hOpacityAnimations;
                    data.hTransform = hTransform;
                    data.hRelativeTransform = hRelativeTransform;
                    data.ColorInterpolationMode = ColorInterpolationMode;
                    data.MappingMode = MappingMode;
                    data.SpreadMethod = SpreadMethod;

                    Point tempCenter = Center;
                    DUCE.CopyBytes((byte*)&data.Center, (byte*)&tempCenter, 16);
                    data.hCenterAnimations = hCenterAnimations;
                    double tempRadiusX = RadiusX;
                    DUCE.CopyBytes((byte*)&data.RadiusX, (byte*)&tempRadiusX, 8);
                    data.hRadiusXAnimations = hRadiusXAnimations;
                    double tempRadiusY = RadiusY;
                    DUCE.CopyBytes((byte*)&data.RadiusY, (byte*)&tempRadiusY, 8);
                    data.hRadiusYAnimations = hRadiusYAnimations;
                    Point tempGradientOrigin = GradientOrigin;
                    DUCE.CopyBytes((byte*)&data.GradientOrigin, (byte*)&tempGradientOrigin, 16);
                    data.hGradientOriginAnimations = hGradientOriginAnimations;

                    // GradientStopCollection:  Need to enforce upper-limit of gradient stop capacity

                    int count = (vGradientStops == null) ? 0 : vGradientStops.Count;
                    data.GradientStopsSize = (UInt32)(sizeof(DUCE.MIL_GRADIENTSTOP)*count);

                    channel.BeginCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_RADIALGRADIENTBRUSH),
                        sizeof(DUCE.MIL_GRADIENTSTOP)*count
                        );
                    
                    for (int i=0; i<count; i++)
                    {
                        DUCE.MIL_GRADIENTSTOP stopCmd;
                        GradientStop gradStop = vGradientStops.Internal_GetItem(i);

                        double temp = gradStop.Offset;
                        DUCE.CopyBytes((byte*)&stopCmd.Position,(byte*)&temp, sizeof(double));
                        stopCmd.Color = CompositionResourceManager.ColorToMilColorF(gradStop.Color);
                        
                        channel.AppendCommandData(
                            (byte*)&stopCmd,
                            sizeof(DUCE.MIL_GRADIENTSTOP)
                            );
                    }
                    
                    channel.EndCommand();
                }
            }
        }
    }
}

