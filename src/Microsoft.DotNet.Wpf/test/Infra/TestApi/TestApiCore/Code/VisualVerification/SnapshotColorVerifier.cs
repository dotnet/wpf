// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// Verifies that all pixels in a Snapshot are within tolerance range of ExpectedColor.
    /// </summary>
    public class SnapshotColorVerifier : SnapshotVerifier
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of a SnapshotColorVerifier class, using black pixels with zero tolerance.
        /// </summary>
        public SnapshotColorVerifier()
        {
            Tolerance = new ColorDifference();
            ExpectedColor = Color.Black;
        }

        /// <summary>
        /// Initializes a new instance of the SnapshotColorVerifier class, using the specified tolerance value. 
        /// </summary>
        /// <param name="expectedColor">The expected color to test against.</param>
        /// <param name="tolerance">A ColorDifference instance specifying the desired tolerance.</param>
        public SnapshotColorVerifier(Color expectedColor, ColorDifference tolerance)
        {
            this.Tolerance = tolerance;
            this.ExpectedColor = expectedColor;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Ensures that the image colors are all within tolerance range of the expected Color.
        /// </summary>
        /// <param name="image">The actual image being verified.</param>
        /// <returns>A VerificationResult enumeration value based on the image, the expected color, and the tolerance.</returns>
        public override VerificationResult Verify(Snapshot image)
        {
            for (int row = 0; row < image.Height; row++)
            {
                for (int column = 0; column < image.Width; column++)
                {
                    ColorDifference diff = image[row, column].Compare(ExpectedColor);
                    if (!diff.MeetsTolerance(Tolerance))
                    {
                        //Exit early as we have a counter-example to prove failure.
                        return VerificationResult.Fail;
                    }
                }
            }
            return VerificationResult.Pass;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// The color tolerance range for verification. To pass verification, all Snapshot pixels must 
        /// be within range of the expected color tolerance.
        /// </summary>
        public ColorDifference Tolerance { get; set; }

        /// <summary>
        /// The expected Color value for verification.
        /// </summary>
        public Color ExpectedColor { get; set; }
        #endregion
    }
}
