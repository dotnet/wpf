// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.RenderingVerification;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// Abstracts framebuffer verification operations for test renderer.
    /// </summary>
    public static class RenderVerifier
    {
        static RenderVerifier()
        {
            int numToleranceLevels = 9;

            toleranceThreshold = new Color[numToleranceLevels];
            codedColor = new Color[numToleranceLevels];
            for (int i = 0; i < numToleranceLevels; i++)
            {
                if (i == 0)
                {
                    toleranceThreshold[i] = Colors.Black;
                }
                else
                {
                    byte val = (byte)Math.Pow(2, i - 1);

                    toleranceThreshold[i] = Color.FromArgb(0x00, val, val, val);
                }
            }
            // Force everything to get caught by last level.
            toleranceThreshold[numToleranceLevels - 1] = Colors.White;

            codedColor[0] = Color.FromArgb(255, 15, 15, 15); // 0 away
            codedColor[1] = Colors.Yellow;    // 1 away
            codedColor[2] = Colors.Orange;    // 2 away
            codedColor[3] = Colors.Red;       // 4 away
            codedColor[4] = Colors.Pink;      // 8 away
            codedColor[5] = Colors.Magenta;   // 16 away
            codedColor[6] = Colors.Cyan;      // 32 away
            codedColor[7] = Colors.LightBlue; // 64 away
            codedColor[8] = Colors.White;     // 128 or > away
        }

        /// <summary>
        /// This function sums over the tolerance buffer and returns what
        /// percentage of an image's color information is being thrown out
        /// (We don't include alpha in our sum)
        /// </summary>
        public static string GetErrorStatistics(RenderBuffer buffer)
        {
            double percentage = 0.0;

            for (int y = 0; y < buffer.Height; y++)
            {
                for (int x = 0; x < buffer.Width; x++)
                {
                    percentage += ColorOperations.ByteToDouble(buffer.ToleranceBuffer[x, y].R);
                    percentage += ColorOperations.ByteToDouble(buffer.ToleranceBuffer[x, y].G);
                    percentage += ColorOperations.ByteToDouble(buffer.ToleranceBuffer[x, y].B);
                }
            }

            // We multiply by 100 for percent, then divide by 3 for "R" "G" and "B"
            percentage = 100.0 * percentage / (3.0 * (double)buffer.Width * (double)buffer.Height);
            return String.Format("Color data ignored due to tolerance criteria: {0}%.", percentage.ToString("##0.00###"));
        }

        /// <summary>
        /// Verifies a screen capture is within tolerance from the rendered buffer.
        /// </summary>
        /// <param name="captured">The captured image.</param>
        /// <param name="expected">The expected image.</param>
        /// <returns>Number of pixels which fail tolerance check. Pass == 0.</returns>
        public static int VerifyRender(Color[,] captured, RenderBuffer expected)
        {
            return VerifyRender(captured, expected, 0, null);
        }

        /// <summary>
        /// Verifies a screen capture is within tolerance from the rendered buffer. If there are any mismatching pixels,
        /// and if the number of mismatches is less than or equal to numAllowableMismatches parameter, then VScan is used
        /// as a secondary verification method to see if ALL of these mismatches can be ignored.
        /// </summary>
        /// <param name="captured">The captured image.</param>
        /// <param name="expected">The expected image.</param>
        /// <param name="numAllowableMismatches">Max number of pixel mismatches that may be ignored.</param>
        /// <param name="VScanToleranceFile">Path to a tolerance file to be used by VScan. If NULL, VScan will use a default tolerance.</param>
        /// <returns>Number of pixels which fail tolerance check. Pass == 0.</returns>
        public static int VerifyRender(Color[,] captured, RenderBuffer expected, int numAllowableMismatches, string VScanToleranceFile)
        {
            Point[] failedPoints = GetPointsWithFailures(captured, expected); // Get the mismatching points
            int failures = (failedPoints != null) ? failedPoints.Length : 0;

            // If there are more mismatches than allowed then we won't use VScan and we'll just mark the test as failed.
            // If the number of mismatches are less than or equal to numAllowableMismatches then we will use VScan to
            // see how many of them will actually be ignored based on the input tolerance profile. If any of them does not
            // meet the tolerance profile then the test is marked as failed. But if ALL of them meet the tolerance profile
            // then they are ignored and the test is marked as passed.
            if (failures > 0 && numAllowableMismatches > 0 && failures <= numAllowableMismatches)
            {
                RenderBuffer diff = ComputeDifference(captured, expected);
                failures = VerifyDifferenceUsingVScan(diff, VScanToleranceFile);
            }

            return failures;
        }

        /// <summary>
        /// Given a difference buffer and an array of failed points, this method uses VScan utility to go through
        /// the failed points and determine how many of them can be ignored.
        /// </summary>
        /// <param name="diffBuffer">The difference buffer.</param>
        /// <param name="failedPoints">The array of failed points.</param>
        /// <returns>Number of pixels that CANNOT be ignored. Pass == 0.</returns>
        private static int VerifyDifferenceUsingVScan(RenderBuffer diffBuffer, string VScanToleranceFile)
        {
            int failures = 0;
            ImageComparator comparator;

            if (VScanToleranceFile != null && File.Exists(VScanToleranceFile)) // using custom tolerance
            {
                CurveTolerance tolerance = new CurveTolerance();
                tolerance.LoadTolerance(VScanToleranceFile);
                comparator = new ImageComparator(tolerance);
            }
            else // using default tolerance;
            {
                comparator = new ImageComparator();
            }

            ImageAdapter blackImageAdapter = new ImageAdapter(diffBuffer.Width, diffBuffer.Height, ColorToIColor(Colors.Black));
            ImageAdapter diffImageAdapter = new ImageAdapter(diffBuffer.Width, diffBuffer.Height);
            for (int x = 0; x < diffBuffer.Width; x++)
            {
                for (int y = 0; y < diffBuffer.Height; y++)
                {
                    diffImageAdapter[x, y] = ColorToIColor(diffBuffer.FrameBuffer[x, y]);
                }
            }

            bool passed = comparator.Compare(blackImageAdapter, diffImageAdapter, true);
            failures = (passed == false && comparator.MismatchingPoints != null) ? comparator.MismatchingPoints.NumMismatchesAboveLevel(1) : 0;

            return failures;
        }

        private static IColor ColorToIColor(Color color)
        {
            return new ColorByte(color.A, color.R, color.G, color.B); 
        }

        /// <summary>
        /// Get only the points where issues were found
        /// If the captured image is smaller than the expected image, throw an exception and refuse to compare them.
        /// Otherwise, compare the expected image with the matching region (the upper left corner) of the rendered image.
        /// We'll do the comparison by x,y coordinates and not pointer math (ie: y*width +x) to ensure correct matching.
        /// </summary>
        /// <returns>Array of points where failures ocurred</returns>
        public static Point[] GetPointsWithFailures(Color[,] captured, RenderBuffer expected)
        {
            //we'll do the comparison by x,y coordinates and not pointer math (ie: y*width +x) to ensure correct matching.
            if (expected.Width > captured.GetLength(0) || expected.Height > captured.GetLength(1))
            {
                throw new ApplicationException(exceptionCapturedRenderedEqual);
            }

            System.Collections.ArrayList failures = new System.Collections.ArrayList();
            Point[] failPoints;
            for (int y = 0; y < expected.Height; y++)
            {
                for (int x = 0; x < expected.Width; x++)
                {
                    if (!ColorOperations.AreWithinTolerance(
                            captured[x, y], expected.FrameBuffer[x, y], expected.ToleranceBuffer[x, y]))
                    {
                        failures.Add(new Point(x, y));
                    }
                }
            }
            // Always return an array, even an empty one
            failPoints = new Point[failures.Count];
            if (failures.Count != 0)
            {
                failures.CopyTo(failPoints);
            }
            return failPoints;
        }

        /// <summary>
        /// Produce a Difference image from a screen capture and a RenderBuffer. For every pixel, if it is an exact match or 
        /// if the difference is within the provided tolerance, the pixel is marked as black. Otherwise the diff value is used.
        /// If the captured image is smaller than the expected image, throw an exception and refuse to compare them.
        /// Otherwise, compare the expected image with the matching region (the upper left corner) of the rendered image.
        /// We'll do the comparison by x,y coordinates and not pointer math (ie: y*width +x) to ensure correct matching.
        /// </summary>
        /// <returns>A new Render buffer with the Diff image on the framebuffer and a color coded image on the tbuffer.</returns>
        public static RenderBuffer ComputeDifference(Color[,] captured, RenderBuffer expected)
        {
            if (expected.Width > captured.GetLength(0) || expected.Height > captured.GetLength(1))
            {
                throw new ApplicationException(exceptionCapturedRenderedEqual);
            }
            

            RenderBuffer result = new RenderBuffer(expected.Width, expected.Height);
            // We want to write to this directly, set z-test to always write ...
            result.DepthTestFunction = DepthTestFunction.Always;
            // We want to ignore any potential z-tolerance as well ...

            for (int y = 0; y < result.Height; y++)
            {
                for (int x = 0; x < result.Width; x++)
                {
                    // Ignore alpha differences.
                    Color diff = ColorOperations.AbsoluteDifference(expected.FrameBuffer[x, y], captured[x, y]);
                    diff.A = 0xff;
                    result.FrameBuffer[x, y] = diff;

                    // Make perfect matches black
                    if (ColorOperations.AreWithinTolerance(captured[x, y], expected.FrameBuffer[x, y], Colors.Black))
                    {
                        result.ToleranceBuffer[x, y] = Colors.Black;
                        result.FrameBuffer[x, y] = Colors.Black;
                    }
                    // Treat within tolerance as separate case
                    else if (ColorOperations.AreWithinTolerance(captured[x, y], expected.FrameBuffer[x, y], expected.ToleranceBuffer[x, y]))
                    {
                        result.ToleranceBuffer[x, y] = codedColor[0];
                        result.FrameBuffer[x, y] = Colors.Black;
                    }
                    // Otherwise do color coding
                    else
                    {
                        for (int i = 1; i < codedColor.Length; i++)
                        {
                            if (ColorOperations.AreWithinTolerance(
                                    captured[x, y],
                                    expected.FrameBuffer[x, y],
                                    ColorOperations.Add(toleranceThreshold[i], expected.ToleranceBuffer[x, y])))
                            {
                                result.ToleranceBuffer[x, y] = codedColor[i];
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        static Color[] toleranceThreshold;
        static Color[] codedColor;

        const string exceptionCapturedRenderedEqual = "Captured image must be of greater than or equal size to expected image to be compared.";
    }
}
