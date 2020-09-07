// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: This file contains the implementation of LinearGradientBrush.
//              The LinearGradientBrush is a GradientBrush which defines its
//              Gradient as a linear interpolation between two parallel lines.
//
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using System.Windows.Media.Composition;
using System.Security;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// LinearGradientBrush - This GradientBrush defines its Gradient as an interpolation
    /// between two parallel lines.
    /// </summary>
    public sealed partial class LinearGradientBrush : GradientBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for LinearGradientBrush.  The resulting brush has no content.
        /// </summary>
        public LinearGradientBrush() : base()
        {
        }

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with GradientStops specified at offset 0.0 and 
        /// 1.0. The StartPoint is set to (0,0) and the EndPoint is derived from the angle 
        /// such that 1) the line containing the StartPoint and EndPoint is 'angle' degrees 
        /// from the horizontal in the direction of positive Y, and 2) the EndPoint lies on 
        ///  the perimeter of the unit circle.
        /// </summary>
        /// <param name="startColor"> The Color at offset 0.0. </param>
        /// <param name="endColor"> The Color at offset 1.0. </param>
        /// <param name="angle"> The angle, in degrees, that the gradient will be away from horizontal. </param>
        public LinearGradientBrush(Color startColor,
                                   Color endColor,
                                   double angle) : base()
        {
            EndPoint = EndPointFromAngle(angle);

            GradientStops.Add(new GradientStop(startColor, 0.0));
            GradientStops.Add(new GradientStop(endColor, 1.0));
        }

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with two colors at the specified start and end points.
        /// </summary>
        /// <param name="startColor"> The Color at offset 0.0. </param>
        /// <param name="endColor"> The Color at offset 1.0. </param>
        /// <param name="startPoint"> The start point</param>
        /// <param name="endPoint"> The end point</param>
        public LinearGradientBrush(Color startColor,
                                   Color endColor,
                                   Point startPoint,
                                   Point endPoint) : base()
        {
            StartPoint = startPoint;
            EndPoint = endPoint;

            GradientStops.Add(new GradientStop(startColor, 0.0));
            GradientStops.Add(new GradientStop(endColor, 1.0));
        }

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with GradientStops set to the passed-in collection.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        public LinearGradientBrush(GradientStopCollection gradientStopCollection)
                                                : base (gradientStopCollection)                                                
        {
        }        

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with GradientStops set to the passed-in collection.
        /// Constructs a LinearGradientBrush with GradientStops specified at offset 0.0 and 
        /// 1.0. The StartPoint is set to (0,0) and the EndPoint is derived from the angle 
        /// such that 1) the line containing the StartPoint and EndPoint is 'angle' degrees 
        /// from the horizontal in the direction of positive Y, and 2) the EndPoint lies on 
        ///  the perimeter of the unit circle.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        /// <param name="angle"> The angle, in degrees, that the gradient will be away from horizontal. </param>        
        public LinearGradientBrush(GradientStopCollection gradientStopCollection,
                                   double angle) : base (gradientStopCollection)
        {
            EndPoint = EndPointFromAngle(angle);
        }        

        /// <summary>
        /// LinearGradientBrush Constructor
        /// Constructs a LinearGradientBrush with GradientStops set to the passed-in collection.
        /// The StartPoint and EndPoint are set to the specified startPoint and endPoint.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        /// <param name="startPoint"> The start point</param>
        /// <param name="endPoint"> The end point</param>
        public LinearGradientBrush(GradientStopCollection gradientStopCollection,
                                   Point startPoint,
                                   Point endPoint) : base (gradientStopCollection)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
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
                DUCE.ResourceHandle hStartPointAnimations = GetAnimationResourceHandle(StartPointProperty, channel);
                DUCE.ResourceHandle hEndPointAnimations = GetAnimationResourceHandle(EndPointProperty, channel);

                unsafe
                {
                    DUCE.MILCMD_LINEARGRADIENTBRUSH data;
                    data.Type = MILCMD.MilCmdLinearGradientBrush;
                    data.Handle = _duceResource.GetHandle(channel);
                    double tempOpacity = Opacity;
                    DUCE.CopyBytes((byte*)&data.Opacity, (byte*)&tempOpacity, 8);
                    data.hOpacityAnimations = hOpacityAnimations;
                    data.hTransform = hTransform;
                    data.hRelativeTransform = hRelativeTransform;
                    data.ColorInterpolationMode = ColorInterpolationMode;
                    data.MappingMode = MappingMode;
                    data.SpreadMethod = SpreadMethod;

                    Point tempStartPoint = StartPoint;
                    DUCE.CopyBytes((byte*)&data.StartPoint, (byte*)&tempStartPoint, 16);
                    data.hStartPointAnimations = hStartPointAnimations;
                    Point tempEndPoint = EndPoint;
                    DUCE.CopyBytes((byte*)&data.EndPoint, (byte*)&tempEndPoint, 16);
                    data.hEndPointAnimations = hEndPointAnimations;

                    // GradientStopCollection:  Need to enforce upper-limit of gradient stop capacity

                    int count = (vGradientStops == null) ? 0 : vGradientStops.Count;
                    data.GradientStopsSize = (UInt32)(sizeof(DUCE.MIL_GRADIENTSTOP)*count);
                    
                    channel.BeginCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_LINEARGRADIENTBRUSH),
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

        private Point EndPointFromAngle(double angle)
        {
            // Convert the angle from degrees to radians
            angle = angle * (1.0/180.0) * System.Math.PI;

            return (new Point(System.Math.Cos(angle), System.Math.Sin(angle)));
}
    }
}

