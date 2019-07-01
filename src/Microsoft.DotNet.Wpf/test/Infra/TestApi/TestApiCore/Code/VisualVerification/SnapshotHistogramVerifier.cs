// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// Verifies a diffed image based on the number of pixels of a given brightness per color. 
    /// A tolerance Histogram curve can be created from an XML file, produced from a reference image, or manually created for use as a tolerance.
    /// <p/>
    /// For more information on histograms, refer to the description of <see cref="Histogram"/>.
    /// </summary>
    ///
    /// <example>
    /// This examples shows how to verify a snapshot against an expected master image, using
    /// a tolerance histogram.
    /// <code>
    /// Snapshot actual = Snapshot.FromRectangle(new Rectangle(0, 0, 800, 600));
    /// Snapshot expected = Snapshot.FromFile("Expected.bmp");
    /// Snapshot diff = actual.CompareTo(expected);
    ///
    /// SnapshotVerifier v = new SnapshotHistogramVerifier(Histogram.FromFile("ToleranceHistogram.xml"));
    ///
    /// if (v.Verify(diff) == VerificationResult.Fail)
    /// {
    ///     diff.ToFile("Actual.bmp", ImageFormat.Bmp);
    /// }
    /// </code>
    /// </example>
    public class SnapshotHistogramVerifier : SnapshotVerifier
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the SnapshotHistgramVerifier class, with the tolerance histogram curve initialized to zero tolerance for non-black values.
        /// </summary>
        public SnapshotHistogramVerifier()
        {
            Tolerance = new Histogram();
        }

        /// <summary>
        /// Initializes a new instance of the SnapshotHistgramVerifier class, with the tolerance histogram curve initialized to the specified tolerance value.
        /// </summary>
        /// <param name="tolerance">The tolerance Histogram to use for verification.</param>
        public SnapshotHistogramVerifier(Histogram tolerance)
        {
            this.Tolerance = tolerance;
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Verifies a diffed image based on the number of pixels of a given brightness per color. 
        /// A tolerance Histogram curve can be created from an XML file, produced from a reference image, or manually created for use as a tolerance.
        /// </summary>
        /// <param name="image">The actual Snapshot to be verified.</param>
        /// <returns>A VerificationResult enumeration value based on the image, the expected color, and the tolerance.</returns>
        public override VerificationResult Verify(Snapshot image)
        {
            Histogram actual = Histogram.FromSnapshot(image);
            return (actual.IsLessThan(Tolerance)) ? VerificationResult.Pass : VerificationResult.Fail;
        }

        /// <summary>
        /// The tolerance Histogram that is used to test snapshots; snapshots must produce a histogram which falls below this curve in order to pass.
        /// </summary>
        public Histogram Tolerance { get; set; }
        #endregion
    }
}
