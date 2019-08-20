// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal
{
    using System;
    using System.Windows;

    /// <summary>
    /// Wrapper for <see cref="DpiScale"/> with supporting utility methods
    /// </summary>
    internal class DpiScale2 : IEquatable<DpiScale2>, IEquatable<DpiScale>
    {
        private DpiScale dpiScale;

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiScale2"/> class.
        /// </summary>
        /// <param name="dpiScale"><see cref="DpiScale"/> instance</param>
        internal DpiScale2(DpiScale dpiScale)
        {
            this.dpiScale = dpiScale;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiScale2"/> class.
        /// </summary>
        /// <param name="dpiScaleX">DPI scale on X-axis</param>
        /// <param name="dpiScaleY">DPI scale on Y-axis</param>
        internal DpiScale2(double dpiScaleX, double dpiScaleY)
            : this(new DpiScale(dpiScaleX, dpiScaleY))
        {
        }

        /// <summary>
        /// Gets the DPI scale on the X-axis
        /// </summary>
        internal double DpiScaleX
        {
            get { return this.dpiScale.DpiScaleX; }
        }

        /// <summary>
        /// Gets the DPI-scale on the Y-axis
        /// </summary>
        internal double DpiScaleY
        {
            get { return this.dpiScale.DpiScaleY; }
        }

        /// <summary>
        /// Gets the PixelsPerDpi at which text should be rendered
        /// </summary>
        internal double PixelsPerDip
        {
            get { return this.dpiScale.PixelsPerDip; }
        }

        /// <summary>
        /// Gets the PPI along X-axis
        /// </summary>
        internal double PixelsPerInchX
        {
            get { return this.dpiScale.PixelsPerInchX; }
        }

        /// <summary>
        /// Gets the PPI along Y-axis
        /// </summary>
        internal double PixelsPerInchY
        {
            get { return this.dpiScale.PixelsPerInchY; }
        }

        /// <summary>
        /// Implicitly casts a <see cref="DpiScale2"/> object to
        /// a <see cref="DpiScale"/> object.
        /// </summary>
        /// <param name="dpiScale2">The <see cref="DpiScale2"/> object that is
        /// being cast</param>
        public static implicit operator DpiScale(DpiScale2 dpiScale2)
        {
            return dpiScale2.dpiScale;
        }

        /// <summary>
        /// Checks to inequality between two <see cref="DpiScale2"/> instances.
        /// </summary>
        /// <param name="dpiScaleA">The first object being compared</param>
        /// <param name="dpiScaleB">The second object being compared</param>
        /// <returns>True if the objects are not equal, otherwise False</returns>
        public static bool operator !=(DpiScale2 dpiScaleA, DpiScale2 dpiScaleB)
        {
            if ((object.ReferenceEquals(dpiScaleA, null) && !object.ReferenceEquals(dpiScaleB, null)) ||
                (!object.ReferenceEquals(dpiScaleA, null) && object.ReferenceEquals(dpiScaleB, null)))
            {
                return true;
            }

            return !dpiScaleA.Equals(dpiScaleB);
        }

        /// <summary>
        /// Checks for equality between two <see cref="DpiScale2"/> instances.
        /// </summary>
        /// <param name="dpiScaleA">The first object being compared</param>
        /// <param name="dpiScaleB">The second object being compared</param>
        /// <returns>True if the two objects are equal, otherwise False</returns>
        public static bool operator ==(DpiScale2 dpiScaleA, DpiScale2 dpiScaleB)
        {
            if (object.ReferenceEquals(dpiScaleA, null) && 
                object.ReferenceEquals(dpiScaleB, null))
            {
                return true;
            }

            return dpiScaleA.Equals(dpiScaleB);
        }

        /// <summary>
        /// Equality test against a <see cref="DpiScale"/> object.
        /// </summary>
        /// <param name="dpiScale">The object being compared against</param>
        /// <returns>True if the objects are equal, False otherwise</returns>
        public bool Equals(DpiScale dpiScale)
        {
            return
                DoubleUtil.AreClose(this.DpiScaleX, dpiScale.DpiScaleX) &&
                DoubleUtil.AreClose(this.DpiScaleY, dpiScale.DpiScaleY);
}

        /// <summary>
        /// Equality test against a <see cref="DpiScale2"/> object.
        /// </summary>
        /// <param name="dpiScale2">The object being compared against</param>
        /// <returns>True if the objects are equal, False otherwise</returns>
        /// <remarks>
        /// Two DPI scale values are equal if they are equal after rounding up
        /// to hundredths place.
        ///
        /// Common PPI values in use are:
        /// <list type="table">
        /// <listheader><term>PPI</term><term>DPI(%)</term><term>DPI(Ratio)</term></listheader>
        /// <item><term>96</term><term>100%</term><term>1.00</term></item>
        /// <item><term>120</term><term>125%</term><term>1.25</term></item>
        /// <item><term>144</term><term>150%</term><term>1.50</term></item>
        /// <item><term>192</term><term>200%</term><term>2.00</term></item>
        /// </list>
        /// </remarks>
        public bool Equals(DpiScale2 dpiScale2)
        {
            if (object.ReferenceEquals(dpiScale2, null))
            {
                return false;
            }

            return this.Equals(dpiScale2.dpiScale);
        }

        /// <summary>
        /// Equality test
        /// </summary>
        /// <param name="obj">The object being compared against</param>
        /// <returns>True if the objects are equal, False otherwise</returns>
        public override bool Equals(object obj)
        {
            bool areEqual = false;

            if (obj is DpiScale)
            {
                areEqual = this.Equals((DpiScale)obj);
            }
            else if (obj is DpiScale2)
            {
                areEqual = this.Equals((DpiScale2)obj);
            }
            else
            {
                areEqual = base.Equals(obj);
            }

            return areEqual;
        }

        /// <summary>
        /// Returns the hash code of the current object
        /// </summary>
        /// <returns>The hash code of the object</returns>
        public override int GetHashCode()
        {
            return ((int)(this.PixelsPerInchX)).GetHashCode();
        }

        /// <summary>
        /// Creates an instances of <see cref="DpiScale2"/> from PPI values.
        /// </summary>
        /// <param name="ppiX">PPI along X-axis</param>
        /// <param name="ppiY">PPI along Y-axis</param>
        /// <returns>A new <see cref="DpiScale2"/> instance</returns>
        internal static DpiScale2 FromPixelsPerInch(double ppiX, double ppiY)
        {
            if (DoubleUtil.LessThanOrClose(ppiX,0) || DoubleUtil.LessThanOrClose(ppiY, 0))
            {
                return null;
            }

            return new DpiScale2(ppiX / DpiUtil.DefaultPixelsPerInch, ppiY / DpiUtil.DefaultPixelsPerInch);
        }
    }
}
