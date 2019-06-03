// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows
{
    /// <summary>
    /// Stores DPI information from which a <see cref="System.Windows.Media.Visual"/> 
    /// or <see cref="System.Windows.UIElement"/> is rendered.
    /// </summary>
    public struct DpiScale
    {
        /// <summary>
        /// Initializes a new instance of the DpiScale structure.
        /// </summary>
        public DpiScale(double dpiScaleX, double dpiScaleY)
        {
            _dpiScaleX = dpiScaleX;
            _dpiScaleY = dpiScaleY;
        }

        /// <summary>
        /// Gets the DPI scale on the X axis.When DPI is 96, <see cref="DpiScaleX"/> is 1. 
        /// </summary>
        /// <remarks>
        /// On Windows Desktop, this value is the same as <see cref="DpiScaleY"/>
        /// </remarks>
        public double DpiScaleX
        {
            get { return _dpiScaleX; }
        }

        /// <summary>
        /// Gets the DPI scale on the Y axis. When DPI is 96, <see cref="DpiScaleY"/> is 1. 
        /// </summary>
        /// <remarks>
        /// On Windows Desktop, this value is the same as <see cref="DpiScaleX"/>
        /// </remarks>
        public double DpiScaleY
        {
            get { return _dpiScaleY; }
        }

        /// <summary>
        /// Get or sets the PixelsPerDip at which the text should be rendered.
        /// </summary>
        public double PixelsPerDip
        {
            get { return _dpiScaleY; }
        }

        /// <summary>
        /// Gets the PPI along X axis.
        /// </summary>
        /// <remarks>
        /// On Windows Desktop, this value is the same as <see cref="PixelsPerInchY"/>
        /// </remarks>
        public double PixelsPerInchX
        {
            get { return DpiUtil.DefaultPixelsPerInch * _dpiScaleX; }
        }

        /// <summary>
        /// Gets the PPI along Y axis.
        /// </summary>
        /// <remarks>
        /// On Windows Desktop, this value is the same as <see cref="PixelsPerInchX"/>
        /// </remarks>
        public double PixelsPerInchY
        {
            get { return DpiUtil.DefaultPixelsPerInch * _dpiScaleY; }
        }

        private readonly double _dpiScaleX;
        private readonly double _dpiScaleY;
    }
}
