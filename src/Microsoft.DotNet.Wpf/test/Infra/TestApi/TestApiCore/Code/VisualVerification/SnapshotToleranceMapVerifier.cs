// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// Verifies that all pixels in a Snapshot are within the tolerance range, defined by the tolerance map.
    /// </summary>
    /// <example>
    /// The following code demonstrates how to use SnapshotToleranceMapVerifier 
    /// for visual verification purposes.
    /// <code>
    /// // Take a snapshot, compare to the master image and generate a diff
    /// WindowSnapshotMode wsm = WindowSnapshotMode.ExcludeWindowBorder;
    ///
    /// Snapshot actual = Snapshot.FromWindow(hwndOfYourWindow, wsm);
    /// Snapshot expected = Snapshot.FromFile("Expected.png");
    /// Snapshot difference = actual.CompareTo(expected);
    ///
    /// // Load the tolerance map. Then use it to verify the difference snapshot
    /// Snapshot toleranceMap = Snapshot.FromFile("ExpectedImageToleranceMap.png");
    /// SnapshotVerifier v = new SnapshotToleranceMapVerifier(toleranceMap);
    ///
    /// if (v.Verify(difference) == VerificationResult.Fail)
    /// {
    ///     // Log failure, and save the actual and diff images for investigation
    ///     actual.ToFile("Actual.png", ImageFormat.Png);
    ///     difference.ToFile("Difference.png", ImageFormat.Png);
    /// }
    /// </code>
    /// </example>
    public class SnapshotToleranceMapVerifier : SnapshotVerifier
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SnapshotToleranceMapVerifier class, using the specified tolerance map. 
        /// </summary>
        /// <param name="toleranceMap">
        /// A Snapshot instance defining the tolerance map, used by the verifier.
        /// A black tolerance map (a snapshot, where all pixels are with zero values) means zero tolerance. 
        /// A white tolerance map (a snapshot, where all pixels are with value 0xFF) means infinitely high tolerance.
        /// </param>
        public SnapshotToleranceMapVerifier(Snapshot toleranceMap)
        {
            this.ToleranceMap = toleranceMap;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ensures that the image colors are all with smaller values than the image colors of the tolerance map.
        /// </summary>
        /// <param name="image">The actual image being verified.</param>
        /// <returns>A VerificationResult enumeration value based on the image, and the tolerance map.</returns>
        public override VerificationResult Verify(Snapshot image)
        {
            if (image.Width != ToleranceMap.Width || image.Height != ToleranceMap.Height)
            {
                throw new InvalidOperationException("image size must match expected size.");
            }

            for (int row = 0; row < image.Height; row++)
            {
                for (int column = 0; column < image.Width; column++)
                {
                    if (image[row, column].A > ToleranceMap[row, column].A ||
                        image[row, column].R > ToleranceMap[row, column].R ||
                        image[row, column].G > ToleranceMap[row, column].G ||
                        image[row, column].B > ToleranceMap[row, column].B)
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
        /// A Snapshot defining the tolerance map used by the verifier.
        /// A black tolerance map (a snapshot, where all pixels are with zero values) means zero tolerance. 
        /// A white tolerance map (a snapshot, where all pixels are with value 0xFF) means infinitely high tolerance.
        /// </summary>
        public Snapshot ToleranceMap { get; set; }

        #endregion
    }
}
