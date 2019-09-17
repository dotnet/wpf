// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

namespace System.Windows.Input
{
    internal static class StylusPointPropertyInfoDefaults
    {
        /// <summary>
        /// X
        /// </summary>
        internal static readonly StylusPointPropertyInfo X =
                new StylusPointPropertyInfo(StylusPointProperties.X,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.Centimeters,
                                            1000.0f);

        /// <summary>
        /// Y
        /// </summary>
        internal static readonly StylusPointPropertyInfo Y =
                new StylusPointPropertyInfo(StylusPointProperties.Y,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.Centimeters,
                                            1000.0f);

        /// <summary>
        /// Z
        /// </summary>
        internal static readonly StylusPointPropertyInfo Z =
                new StylusPointPropertyInfo(StylusPointProperties.Z,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.Centimeters,
                                            1000.0f);

        /// <summary>
        /// Width
        /// </summary>
        internal static readonly StylusPointPropertyInfo Width =
                new StylusPointPropertyInfo(StylusPointProperties.Width,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.Centimeters,
                                            1000.0f);

        /// <summary>
        /// Height
        /// </summary>
        internal static readonly StylusPointPropertyInfo Height =
                new StylusPointPropertyInfo(StylusPointProperties.Height,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.Centimeters,
                                            1000.0f);

        /// <summary>
        /// SystemTouch
        /// </summary>
        internal static readonly StylusPointPropertyInfo SystemTouch =
                new StylusPointPropertyInfo(StylusPointProperties.SystemTouch,
                                            0,
                                            1,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// PacketStatus
        /// </summary>
        internal static readonly StylusPointPropertyInfo PacketStatus =
                new StylusPointPropertyInfo(StylusPointProperties.PacketStatus,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// SerialNumber 
        /// </summary>
        /// <remarks>
        internal static readonly StylusPointPropertyInfo SerialNumber =
                new StylusPointPropertyInfo(StylusPointProperties.SerialNumber,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// NormalPressure
        /// </summary>
        internal static readonly StylusPointPropertyInfo NormalPressure =
                new StylusPointPropertyInfo(StylusPointProperties.NormalPressure,
                                            0,
                                            1023,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// TangentPressure
        /// </summary>
        internal static readonly StylusPointPropertyInfo TangentPressure =
                new StylusPointPropertyInfo(StylusPointProperties.TangentPressure,
                                            0,
                                            1023,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// ButtonPressure
        /// </summary>
        internal static readonly StylusPointPropertyInfo ButtonPressure =
                new StylusPointPropertyInfo(StylusPointProperties.ButtonPressure,
                                            0,
                                            1023,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// XTiltOrientation
        /// </summary>
        internal static readonly StylusPointPropertyInfo XTiltOrientation =
                new StylusPointPropertyInfo(StylusPointProperties.XTiltOrientation,
                                            0,
                                            3600,
                                            StylusPointPropertyUnit.Degrees,
                                            10.0f);

        /// <summary>
        /// YTiltOrientation
        /// </summary>
        internal static readonly StylusPointPropertyInfo YTiltOrientation =
                new StylusPointPropertyInfo(StylusPointProperties.YTiltOrientation,
                                            0,
                                            3600,
                                            StylusPointPropertyUnit.Degrees,
                                            10.0f);

        /// <summary>
        /// AzimuthOrientation
        /// </summary>
        internal static readonly StylusPointPropertyInfo AzimuthOrientation =
                new StylusPointPropertyInfo(StylusPointProperties.AzimuthOrientation,
                                            0,
                                            3600,
                                            StylusPointPropertyUnit.Degrees,
                                            10.0f);

        /// <summary>
        /// AltitudeOrientation
        /// </summary>
        internal static readonly StylusPointPropertyInfo AltitudeOrientation =
                new StylusPointPropertyInfo(StylusPointProperties.AltitudeOrientation,
                                            -900,
                                            900,
                                            StylusPointPropertyUnit.Degrees,
                                            10.0f);

        /// <summary>
        /// TwistOrientation
        /// </summary>
        internal static readonly StylusPointPropertyInfo TwistOrientation =
                new StylusPointPropertyInfo(StylusPointProperties.TwistOrientation,
                                            0,
                                            3600,
                                            StylusPointPropertyUnit.Degrees,
                                            10.0f);

        /// <summary>
        /// PitchRotation
        /// </summary>
        internal static readonly StylusPointPropertyInfo PitchRotation =
                new StylusPointPropertyInfo(StylusPointProperties.PitchRotation,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// RollRotation
        /// </summary>
        internal static readonly StylusPointPropertyInfo RollRotation =
                new StylusPointPropertyInfo(StylusPointProperties.RollRotation,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// YawRotation
        /// </summary>
        internal static readonly StylusPointPropertyInfo YawRotation =
                new StylusPointPropertyInfo(StylusPointProperties.YawRotation,
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// TipButton 
        /// </summary>
        internal static readonly StylusPointPropertyInfo TipButton =
                new StylusPointPropertyInfo(StylusPointProperties.TipButton,
                                            0,
                                            1,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// BarrelButton 
        /// </summary>
        internal static readonly StylusPointPropertyInfo BarrelButton =
                new StylusPointPropertyInfo(StylusPointProperties.BarrelButton,
                                            0,
                                            1,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// SecondaryTipButton 
        /// </summary>
        internal static readonly StylusPointPropertyInfo SecondaryTipButton =
                new StylusPointPropertyInfo(StylusPointProperties.SecondaryTipButton,
                                            0,
                                            1,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// Default Value
        /// </summary>
        internal static readonly StylusPointPropertyInfo DefaultValue =
                new StylusPointPropertyInfo(new StylusPointProperty(Guid.NewGuid(), false),
                                            Int32.MinValue,
                                            Int32.MaxValue,
                                            StylusPointPropertyUnit.None,
                                            1.0F);

        /// <summary>
        /// DefaultButton 
        /// </summary>
        internal static readonly StylusPointPropertyInfo DefaultButton =
                new StylusPointPropertyInfo(new StylusPointProperty(Guid.NewGuid(), true),
                                            0,
                                            1,
                                            StylusPointPropertyUnit.None,
                                            1.0f);

        /// <summary>
        /// For a given StylusPointProperty, return the default property info
        /// </summary>
        /// <param name="stylusPointProperty">stylusPointProperty</param>
        /// <returns></returns>
        internal static StylusPointPropertyInfo GetStylusPointPropertyInfoDefault(StylusPointProperty stylusPointProperty)
        {
            if (stylusPointProperty.Id == StylusPointPropertyIds.X)
            {
                return StylusPointPropertyInfoDefaults.X;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.Y)
            {
                return StylusPointPropertyInfoDefaults.Y;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.Z)
            {
                return StylusPointPropertyInfoDefaults.Z;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.Width)
            {
                return StylusPointPropertyInfoDefaults.Width;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.Height)
            {
                return StylusPointPropertyInfoDefaults.Height;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.SystemTouch)
            {
                return StylusPointPropertyInfoDefaults.SystemTouch;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.PacketStatus)
            {
                return StylusPointPropertyInfoDefaults.PacketStatus;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.SerialNumber)
            {
                return StylusPointPropertyInfoDefaults.SerialNumber;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.NormalPressure)
            {
                return StylusPointPropertyInfoDefaults.NormalPressure;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.TangentPressure)
            {
                return StylusPointPropertyInfoDefaults.TangentPressure;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.ButtonPressure)
            {
                return StylusPointPropertyInfoDefaults.ButtonPressure;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.XTiltOrientation)
            {
                return StylusPointPropertyInfoDefaults.XTiltOrientation;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.YTiltOrientation)
            {
                return StylusPointPropertyInfoDefaults.YTiltOrientation;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.AzimuthOrientation)
            {
                return StylusPointPropertyInfoDefaults.AzimuthOrientation;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.AltitudeOrientation)
            {
                return StylusPointPropertyInfoDefaults.AltitudeOrientation;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.TwistOrientation)
            {
                return StylusPointPropertyInfoDefaults.TwistOrientation;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.PitchRotation)
            {
                return StylusPointPropertyInfoDefaults.PitchRotation;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.RollRotation)
            {
                return StylusPointPropertyInfoDefaults.RollRotation;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.YawRotation)
            {
                return StylusPointPropertyInfoDefaults.YawRotation;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.TipButton)
            {
                return StylusPointPropertyInfoDefaults.TipButton;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.BarrelButton)
            {
                return StylusPointPropertyInfoDefaults.BarrelButton;
            }
            if (stylusPointProperty.Id == StylusPointPropertyIds.SecondaryTipButton)
            {
                return StylusPointPropertyInfoDefaults.SecondaryTipButton;
            }

            //
            // return a default
            //
            if (stylusPointProperty.IsButton)
            {
                return StylusPointPropertyInfoDefaults.DefaultButton;
            }
            return StylusPointPropertyInfoDefaults.DefaultValue;
        }
    }
}
