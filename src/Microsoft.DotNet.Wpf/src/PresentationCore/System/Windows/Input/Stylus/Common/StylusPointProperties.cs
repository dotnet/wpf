// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// StylusPointProperties
    /// </summary>
    public static class StylusPointProperties
    {
        /// <summary>
        /// X
        /// </summary>
        public static readonly StylusPointProperty X =
                new StylusPointProperty( StylusPointPropertyIds.X, false);

        /// <summary>
        /// Y
        /// </summary>
        public static readonly StylusPointProperty Y =
                new StylusPointProperty( StylusPointPropertyIds.Y, false);

        /// <summary>
        /// Z
        /// </summary>
        public static readonly StylusPointProperty Z =
                new StylusPointProperty( StylusPointPropertyIds.Z, false);

        /// <summary>
        /// Width
        /// </summary>
        public static readonly StylusPointProperty Width =
                new StylusPointProperty(StylusPointPropertyIds.Width, false);

        /// <summary>
        /// Height
        /// </summary>
        public static readonly StylusPointProperty Height =
                new StylusPointProperty(StylusPointPropertyIds.Height, false);
        
        /// <summary>
        /// SystemContact
        /// </summary>
        public static readonly StylusPointProperty SystemTouch =
                new StylusPointProperty(StylusPointPropertyIds.SystemTouch, false);

        /// <summary>
        /// PacketStatus
        /// </summary>
        public static readonly StylusPointProperty PacketStatus =
                new StylusPointProperty( StylusPointPropertyIds.PacketStatus, false);

        /// <summary>
        /// SerialNumber
        /// </summary>
        public static readonly StylusPointProperty SerialNumber =
                new StylusPointProperty(StylusPointPropertyIds.SerialNumber, false);

        /// <summary>
        /// NormalPressure
        /// </summary>
        public static readonly StylusPointProperty NormalPressure =
                new StylusPointProperty( StylusPointPropertyIds.NormalPressure, false);

        /// <summary>
        /// TangentPressure
        /// </summary>
        public static readonly StylusPointProperty TangentPressure =
                new StylusPointProperty( StylusPointPropertyIds.TangentPressure, false);

        /// <summary>
        /// ButtonPressure
        /// </summary>
        public static readonly StylusPointProperty ButtonPressure =
                new StylusPointProperty( StylusPointPropertyIds.ButtonPressure, false);

        /// <summary>
        /// XTiltOrientation
        /// </summary>
        public static readonly StylusPointProperty XTiltOrientation =
                new StylusPointProperty( StylusPointPropertyIds.XTiltOrientation, false);

        /// <summary>
        /// YTiltOrientation
        /// </summary>
        public static readonly StylusPointProperty YTiltOrientation =
                new StylusPointProperty( StylusPointPropertyIds.YTiltOrientation, false);

        /// <summary>
        /// AzimuthOrientation
        /// </summary>
        public static readonly StylusPointProperty AzimuthOrientation =
                new StylusPointProperty( StylusPointPropertyIds.AzimuthOrientation, false);

        /// <summary>
        /// AltitudeOrientation
        /// </summary>
        public static readonly StylusPointProperty AltitudeOrientation =
                new StylusPointProperty( StylusPointPropertyIds.AltitudeOrientation, false);

        /// <summary>
        /// TwistOrientation
        /// </summary>
        public static readonly StylusPointProperty TwistOrientation =
                new StylusPointProperty( StylusPointPropertyIds.TwistOrientation, false);

        /// <summary>
        /// PitchRotation
        /// </summary>
        public static readonly StylusPointProperty PitchRotation =
                new StylusPointProperty( StylusPointPropertyIds.PitchRotation, false);

        /// <summary>
        /// RollRotation
        /// </summary>
        public static readonly StylusPointProperty RollRotation =
                new StylusPointProperty(StylusPointPropertyIds.RollRotation, false);

        /// <summary>
        /// YawRotation
        /// </summary>
        public static readonly StylusPointProperty YawRotation =
                new StylusPointProperty(StylusPointPropertyIds.YawRotation, false);

        /// <summary>
        /// TipButton
        /// </summary>
        public static readonly StylusPointProperty TipButton =
                new StylusPointProperty(StylusPointPropertyIds.TipButton, true);

        /// <summary>
        /// BarrelButton
        /// </summary>
        public static readonly StylusPointProperty BarrelButton =
                new StylusPointProperty(StylusPointPropertyIds.BarrelButton, true);

        /// <summary>
        /// SecondaryTipButton
        /// </summary>
        public static readonly StylusPointProperty SecondaryTipButton =
                new StylusPointProperty(StylusPointPropertyIds.SecondaryTipButton, true);
    }
}
