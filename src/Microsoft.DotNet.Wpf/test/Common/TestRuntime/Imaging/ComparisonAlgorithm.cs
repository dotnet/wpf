// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Microsoft.Test.Imaging
{
    /// <summary>
    /// Base class for image comparison algorithms.
    /// </summary>
    /// <remarks>This class is meant to be subclasses by specific comparison
    /// algorithms. Services common to all comparison algorithms can be
    /// added here.</remarks>
    /// <author sdk="True" alias="mruiz">Marcelo</author>
    public abstract class ComparisonAlgorithm
    {
        #region Private data.

		private ComparisonCriteria criteria;
		private Bitmap masterImage;
		private Bitmap sampleImage;

        #endregion Private data.

        #region Public properties.

        /// <summary>
        /// Comparison criteria that define parameters for algorithm.
        /// </summary>
        public ComparisonCriteria Criteria
        {
            get { return this.criteria; }
            set { this.criteria = value; }
        }

        /// <summary>
        /// Master image for comparison.
        /// </summary>
        public Bitmap MasterImage
        {
            get { return this.masterImage; }
            set { this.masterImage = value; }
        }

        /// <summary>
        /// Sampled image for comparison.
        /// </summary>
        public Bitmap SampleImage
        {
            get { return this.sampleImage; }
            set { this.sampleImage = value; }
        }

        #endregion Public properties.

        #region Public methods.

		/// <summary>
		/// Compares the SampleImage to the MasterImage and reports the
		/// comparison result.
		/// </summary>
		/// <returns>The result of the matching operation.</returns>
		public abstract ComparisonResult Execute();

        #endregion Public methods.

        #region Services for subclasses.

        private Size masterSize;
        private Size sampleSize;
        private Rectangle masterRectangle;
        private Rectangle sampleRectangle;
        private int maxErrorCount;

        /// <summary>Number of pixels in master image.</summary>
        protected int MasterPixelCount 
        { 
            get { return MasterImage.Width * MasterImage.Height; }
        }
        
        /// <summary>Size of master image.</summary>
        /// <remarks>Valid after CalculateMetrics call.</remarks>
        protected Size MasterSize { get { return this.masterSize; } }
        
        /// <summary>Maximum number of acceptable errors.</summary>
        protected int MaxErrorCount { get { return this.maxErrorCount; } }
        
        /// <summary>Size of sample image.</summary>
        /// <remarks>Valid after CalculateMetrics call.</remarks>
        protected Size SampleSize { get { return this.sampleSize; } }
        
        /// <summary>Bounding rectangle for master image size.</summary>
        /// <remarks>Valid after CalculateMetrics call.</remarks>
        protected Rectangle MasterRectangle { get { return this.masterRectangle; } }
        
        /// <summary>Bounding rectangel for sample image size.</summary>
        /// <remarks>Valid after CalculateMetrics call.</remarks>
        protected Rectangle SampleRectangle { get { return this.sampleRectangle; } }
        
        /// <summary>Byte count of each scan line in the master image.</summary>
        /// <remarks>Valid after CalculateMetrics call.</remarks>
        protected int MasterScanLineWidth { get { return PixelData.GetScanLineWidth(MasterSize); } }
        
        /// <summary>Byte count of each scan line in the sample image.</summary>
        /// <remarks>Valid after CalculateMetrics call.</remarks>
        protected int SampleScanLineWidth { get { return PixelData.GetScanLineWidth(SampleSize); } }

        /// <summary>
        /// Calculates sizes and other metrics for the given images. Stores
        /// relevant metrics in the result.
        /// </summary>
        /// <param name="result">Comparison result to set metrics on.</param>
        protected void CalculateMetrics(ComparisonResult result)
        {
            // Calculate sizes.
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF masterRect = MasterImage.GetBounds(ref unit);
            this.masterSize = new Size((int) masterRect.Width, (int) masterRect.Height);
            RectangleF sampleRect = SampleImage.GetBounds(ref unit);
            this.sampleSize = new Size((int) sampleRect.Width, (int) sampleRect.Height);
            this.masterRectangle = 
                new Rectangle(0, 0, MasterSize.Width, MasterSize.Height);
            this.sampleRectangle = 
                new Rectangle(0, 0, SampleSize.Width, SampleSize.Height);
            this.maxErrorCount = (int)
                (MasterPixelCount * Criteria.MaxErrorProportion);

            // Add relevant metrics to comparison result.
            result.SetMasterImagePixelCount(MasterPixelCount);
            result.SetErrorProportion(Criteria.MaxErrorProportion);
        }

        /// <summary>
        /// Verifies that the master and sample images match in size.
        /// Otherwise, the difference is added to the result.
        /// </summary>
        /// <param name="result">The comparison result to add the difference
        /// to. May be null.</param>
        /// <returns>true if the sizes match, false otherwise.</returns>
        protected bool RequireSizeMatch(ComparisonResult result)
        {
            if (MasterSize.Equals(SampleSize))
            {
                return true;
            }
            else
            {
                result.AddDifference(
                    new SizeDifference(MasterSize, SampleSize));
                result.SetIdentical(false);
                result.SetCriteriaMet(false);
                return false;
            }
        }

        /// <summary>
        /// Compares to pixels for strict equality.
        /// </summary>
        /// <param name="p1">First pixel to compare.</param>
        /// <param name="other">Second pixel to compare.</param>
        /// <returns>true if the pixels are equal; false otherwise.</returns>
        internal bool PixelsEqual(PixelData p1, PixelData other)
        {
            return (p1.blue == other.blue) && 
                (p1.green == other.green) && (p1.red == other.red);
        }

        internal unsafe PixelData* GetPixelDataAt(BitmapData data,
            int x, int y, int scanWidth)
        {
            PixelData* result = (PixelData*)
                ((byte*)data.Scan0.ToPointer() + y * scanWidth);
            result += x;
            return result;
        }

        /// <summary>
        /// Provides a delta for a pixel position.
        /// </summary>
        internal struct PositionDelta
        {
            public int X;
            public int Y;
            
            /// <summary>
            /// Initializes a PositionDelta structure.
            /// </summary>
            /// <param name="x">Horizontal delta.</param>
            /// <param name="y">Vertical delta.</param>
            internal PositionDelta(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            /// <summary>
            /// Calculates the array of positions that lie at
            /// exactly distance pixels (with grid movements).
            /// </summary>
            /// <param name="distance">Distance to get positions for.</param>
            /// <returns>An array of position deltas from a center.</returns>
            internal static PositionDelta[] ForExactDistance(int distance)
            {
                // Get the easy calculations out of our way first.
                if (distance == 0)
                {
                    return new PositionDelta[] { new PositionDelta(0, 0) };
                }
                
                if (distance == 1)
                {
                    return new PositionDelta[] {
                        new PositionDelta(0, 1),
                        new PositionDelta(1, 0),
                        new PositionDelta(0, -1),
                        new PositionDelta(-1, 0)
                    };
                }

                //
                // Do the real calculation. This is what the distance
                // grid look like:
                //
                // 4 3 2 3 4
                // 3 2 1 2 3
                // 2 1 X 1 2
                // 3 2 1 2 3
                // 4 3 2 3 4
                //
                // Basically, the exact distance is a diamong around
                // the center pixel.
                //
                PositionDelta[] result = new PositionDelta[distance * 4];

                // Fill the "upper" part of the diamond.
                int index = 0;
                for (int x = -distance; x <= distance; x++)
                {
                    result[index] = new PositionDelta(x, distance - x);
                    index++;
                }

                // Fill the "lower" part of the diamond.
                for (int x = (-distance + 1); x <= (distance - 1); x++)
                {
                    result[index] = new PositionDelta(x, x - distance);
                    index++;
                }

                return result;
            }

            /// <summary>
            /// Calculates the array of positions that lie within
            /// distance pixels (with grid movements).
            /// </summary>
            /// <param name="distance">Distance to get positions for.</param>
            /// <returns>An array of position deltas from a center.</returns>
            internal static PositionDelta[] ForWithinDistance(int distance)
            {
                PositionDelta[] result = 
                    new PositionDelta[distance * 4 * distance + 1];
                int index = 0;
                for (int i = 0; i <= distance; i++)
                {
                    PositionDelta[] part = ForExactDistance(i);
                    Array.Copy(part, 0, result, index, part.Length);
                    index += part.Length;
                }
                return result;
            }
        }


        #endregion Services for subclasses.
    }

    /// <summary>
    /// This class performs a pixel-by-pixel comparison of the images.
    /// </summary>
    internal class PerfectMatchAlgorithm: ComparisonAlgorithm
    {
        #region Public methods.

        /// <summary>
        /// Compares the SampleImage to the MasterImage and reports the
        /// comparison result.
        /// </summary>
        /// <returns>The result of the matching operation.</returns>
		public override ComparisonResult Execute()
		{
			ComparisonResult result = new ComparisonResult();
			CalculateMetrics(result);
			if (RequireSizeMatch(result))
			{
                new System.Security.Permissions.SecurityPermission(
                    System.Security.Permissions.PermissionState.Unrestricted)
                    .Assert();
				CompareData(result);
			}
			return result;
		}

        #endregion Public methods.

        #region Private methods.

        private unsafe void CompareData(ComparisonResult result)
		{
			BitmapData sampleData = null;
			BitmapData masterData = MasterImage.LockBits(MasterRectangle,
				ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			try
			{
				sampleData = SampleImage.LockBits(SampleRectangle,
					ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
				int width = MasterScanLineWidth;
				int errorCount = 0;
				PixelData* masterPixels;
				PixelData* samplePixels = (PixelData*) sampleData.Scan0;
				for (int y=0; y < MasterSize.Height; y++)
				{
                    masterPixels = (PixelData*)
                        ((byte*)masterData.Scan0.ToPointer() + y * width);
                    samplePixels = (PixelData*) 
                        ((byte*)sampleData.Scan0.ToPointer() + y * width);
                    for (int x=0; x < MasterSize.Width; x++)
					{
						bool match = PixelsEqual(*masterPixels, *samplePixels);
						if (!match)
						{
							result.AddDifference(
								new PixelColorDifference(x, y,
								masterPixels->red, masterPixels->green, masterPixels->blue,
								samplePixels->red, samplePixels->green, samplePixels->blue));
                            result.SetIdentical(false);
							errorCount++;
							if (errorCount > MaxErrorCount)
							{
								result.SetCriteriaMet(false);
								return;
							}
						}
						masterPixels++;
						samplePixels++;
					}
				}
			}
			finally
			{
				MasterImage.UnlockBits(masterData);
				if (sampleData != null)
					SampleImage.UnlockBits(sampleData);
			}
		}

        #endregion Private methods.
    }

    /// <summary>
    /// This class performs a pixel-by-pixel comparison of the colors in
    /// images.
    /// </summary><remarks>
    /// The color distance is computed as follows:
    /// distance^2 = red_delta^2 + green_delta^2 + blue_delta^2
    /// </remarks>
    internal class ColorComparisonAlgorithm: ComparisonAlgorithm
    {
        #region Public methods.

        /// <summary>
        /// Compares the SampleImage to the MasterImage and reports the
        /// comparison result.
        /// </summary>
        /// <returns>The result of the matching operation.</returns>
        public override ComparisonResult Execute()
        {
            ComparisonResult result = new ComparisonResult();
            CalculateMetrics(result);
            if (RequireSizeMatch(result))
            {
                new System.Security.Permissions.SecurityPermission(
                    System.Security.Permissions.PermissionState.Unrestricted)
                    .Assert();
                CompareData(result);
            }
            return result;
        }

        #endregion Public methods.
        
        #region Private methods.

        private BitmapData sampleData;
        private BitmapData masterData;
        private PositionDelta[] positions;
        private float maxSquareDistance;
        private float minSquareDistance;

        /// <summary>
        /// Checks whether a pixel has an acceptable neighbouring value.
        /// </summary>
        private unsafe void CheckPixel(int x, int y, 
            ComparisonResult result, ref int errorCount)
        {
            //
            // Locals used to mark whether we're below or above range.
            // If both, below wins arbitrarily.
            //
            float belowDistance = 0;
            float aboveDistance = 0;
            bool belowFound = false;
            bool aboveFound = false;

            PixelData* masterPixel = GetPixelDataAt(
                masterData, x, y, MasterScanLineWidth);
            for (int i = 0; i < positions.Length; i++)
            {
                PositionDelta d = positions[i];
                int sampleX = x + d.X;
                int sampleY = y + d.Y;
                if (sampleX < 0 || sampleX > SampleSize.Width) continue;
                if (sampleY < 0 || sampleY > SampleSize.Height) continue;

                PixelData* samplePixel = GetPixelDataAt(
                    sampleData, sampleX, sampleY, SampleScanLineWidth);
                
                float distance = ColorUtils.GetSquareLinearDistance(
                    masterPixel, samplePixel);

                if (minSquareDistance > distance)
                {
                    aboveFound = true;
                    aboveDistance = distance;
                }
                else if (distance > maxSquareDistance)
                {
                    belowFound = true;
                    belowDistance = distance;
                }
                else
                {
                    // Acceptable value found.
                    return;
                }
            }

            if (aboveFound)
            {
                result.AddDifference(
                    new ColorDistanceDifference(x, y,
                    minSquareDistance, aboveDistance, 
                    ValueComparison.AboveValue));
            }
            else
            {
                System.Diagnostics.Debug.Assert(belowFound);
                result.AddDifference(new ColorDistanceDifference(
                    x, y, maxSquareDistance, belowDistance, 
                    ValueComparison.BelowValue));
            }
            errorCount++;
        }

        /// <summary>
        /// Compares the images into the given result.
        /// </summary>
        private unsafe void CompareData(ComparisonResult result)
        {
            sampleData = null;
            masterData = BitmapUtils.LockBitmapDataRead(MasterImage);
            try
            {            
                sampleData = BitmapUtils.LockBitmapDataRead(SampleImage);
                
                int width = MasterScanLineWidth;
                positions = PositionDelta.ForWithinDistance(Criteria.MaxPixelDistance);
                maxSquareDistance = (255 * 255 + 255 * 255 + 255 * 255) * 
                    Criteria.MaxColorDistance;
                minSquareDistance = (255 * 255 + 255 * 255 + 255 * 255) * 
                    Criteria.MinColorDistance;

                int errorCount = 0;
                for (int y=0; y < MasterSize.Height; y++)
                {
                    PixelData * masterPixels = (PixelData*)
                        ((byte*)masterData.Scan0.ToPointer() + y * width);
                    PixelData * samplePixels = (PixelData*) 
                        ((byte*)sampleData.Scan0.ToPointer() + y * width);
                    for (int x=0; x < MasterSize.Width; x++)
                    {
                        bool match = PixelsEqual(*masterPixels, *samplePixels);
                        if (!match || minSquareDistance != 0)
                        {
                            result.SetIdentical(false);
                            CheckPixel(x, y, result, ref errorCount);
                            if (errorCount > MaxErrorCount)
                            {
                                result.SetCriteriaMet(false);
                                return;
                            }
                        }
                        masterPixels++;
                        samplePixels++;
                    }
                }
            }
            finally
            {
                MasterImage.UnlockBits(masterData);
                if (sampleData != null)
                    SampleImage.UnlockBits(sampleData);
                masterData = null;
                sampleData = null;
            }
        }

        #endregion Private methods.
    }
}
