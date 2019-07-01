// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// Represents the per-channel difference between two colors.
    /// </summary>
    public class ColorDifference
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ColorDifference class using values of zero, indicating no difference. 
        /// </summary>
        public ColorDifference()
        {
            A = 0;
            R = 0;
            G = 0;
            B = 0;
        }

        /// <summary>
        /// Initializes a new instance of the ColorDifference class, using the specified alpha, red, green and blue values.
        /// </summary>
        /// <param name="alpha">The alpha (transparency) color channel difference.</param>
        /// <param name="red">The red color channel difference.</param>
        /// <param name="green">The green color channel difference.</param>
        /// <param name="blue">The blue color channel difference.</param>
        public ColorDifference(byte alpha, byte red, byte green, byte blue)
        {
            A = alpha;
            R = red;
            G = green;
            B = blue;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Alpha (transparency) color channel difference.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public byte A { get; set; }

        /// <summary>
        /// Red color channel difference.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public byte R { get; set; }

        /// <summary>
        /// Green color channel difference.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public byte G { get; set; }

        /// <summary>
        /// Blue color channel difference.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public byte B { get; set; }

        #endregion

        #region Internal Members

        /// <summary>
        /// Returns true if this color is less than or equal to reference color on all channels.
        /// </summary>
        /// <param name="reference">The reference color to evaluate against.</param>
        /// <returns>True if this color is less than or equal to reference on all channels.</returns>
        internal bool MeetsTolerance(ColorDifference reference)
        {
            return (this.A <= reference.A) &&
                (this.R <= reference.R) &&
                (this.G <= reference.G) &&
                (this.B <= reference.B);
        }

        #endregion
    }
}
