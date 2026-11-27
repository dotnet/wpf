// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: This file contains the implementation of SweepGradientBrush.
//              The SweepGradientBrush is a GradientBrush which defines its
//              Gradient as a sweep (angular) interpolation around a Center
//              point, from StartAngle to EndAngle (in degrees, clockwise
//              from the positive X axis).
//
//

using System.Windows.Media.Composition;

namespace System.Windows.Media
{
    /// <summary>
    /// SweepGradientBrush - This GradientBrush defines its Gradient as a sweep (angular)
    /// interpolation around a Center point, from StartAngle to EndAngle.
    /// </summary>
    public sealed partial class SweepGradientBrush : GradientBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for SweepGradientBrush.  The resulting brush has no content.
        /// </summary>
        public SweepGradientBrush() : base()
        {
        }

        /// <summary>
        /// SweepGradientBrush Constructor
        /// Constructs a SweepGradientBrush with two colors specified for GradientStops at
        /// offsets 0.0 and 1.0.
        /// </summary>
        /// <param name="startColor"> The Color at offset 0.0. </param>
        /// <param name="endColor"> The Color at offset 1.0. </param>
        public SweepGradientBrush(Color startColor,
                                  Color endColor) : base()
        {
            GradientStops.Add(new GradientStop(startColor, 0.0));
            GradientStops.Add(new GradientStop(endColor, 1.0));
        }

        /// <summary>
        /// SweepGradientBrush Constructor
        /// Constructs a SweepGradientBrush with GradientStops set to the passed-in
        /// collection.
        /// </summary>
        /// <param name="gradientStopCollection"> GradientStopCollection to set on this brush. </param>
        public SweepGradientBrush(GradientStopCollection gradientStopCollection)
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
                DUCE.ResourceHandle hStartAngleAnimations = GetAnimationResourceHandle(StartAngleProperty, channel);
                DUCE.ResourceHandle hEndAngleAnimations = GetAnimationResourceHandle(EndAngleProperty, channel);

                DUCE.MILCMD_SWEEPGRADIENTBRUSH data;
                unsafe
                {
                    data.Type = MILCMD.MilCmdSweepGradientBrush;
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
                    double tempStartAngle = StartAngle;
                    DUCE.CopyBytes((byte*)&data.StartAngle, (byte*)&tempStartAngle, 8);
                    data.hStartAngleAnimations = hStartAngleAnimations;
                    double tempEndAngle = EndAngle;
                    DUCE.CopyBytes((byte*)&data.EndAngle, (byte*)&tempEndAngle, 8);
                    data.hEndAngleAnimations = hEndAngleAnimations;

                    // GradientStopCollection:  Need to enforce upper-limit of gradient stop capacity

                    int count = (vGradientStops == null) ? 0 : vGradientStops.Count;
                    data.GradientStopsSize = (UInt32)(sizeof(DUCE.MIL_GRADIENTSTOP)*count);

                    channel.BeginCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_SWEEPGRADIENTBRUSH),
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
