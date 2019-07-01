// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Imaging
{
	/// <summary>
	/// This class is used to describe the criteria to be used
	/// when matching images.
	/// </summary>
	/// <remarks>
	/// Distances between colors or brightness values
	/// are given as floating-point values between 0 and 1, where
	/// 1 means maximum difference and 0 means no difference.
	/// </remarks>
	public sealed class ComparisonCriteria
	{
		/// <summary>Creates a new ComparisonCriteria instance.</summary>
		public ComparisonCriteria()
		{
			// By default, the comparison criteria should request a perfect
			// match. We allow the user to introduce fuzziness explicitly.
		}

		#region Private data.
		private float maxBrightnessContrast;
		private float maxBrightnessDistance;
		private float maxColorContrast;
		private float maxColorDistance;
		private float maxErrorProportion;
		private int maxPixelDistance;
		private float minColorDistance;
		private float minColorContrast;
		private float minBrightnessDistance;
		private float minBrightnessContrast;
		private string mismatchDescription;
		#endregion Private data.

		#region Public properties.
		/// <summary>Maximum acceptable brightness constrast between pixels.</summary>
		/// <remarks>Brightness contrast is calculated as how contrasting 
		/// the pixel is with regards to surrounding pixels.</remarks>
		public float MaxBrightnessContrast
		{
			get { return this.maxBrightnessContrast; }
			set { this.maxBrightnessContrast = value; }
		}
		/// <summary>Maximum acceptable brightness distance between pixels.</summary>
		public float MaxBrightnessDistance
		{
			get { return this.maxBrightnessDistance; }
			set { this.maxBrightnessDistance = value; }
		}
		/// <summary>Maximum acceptable color contrast between pixels.</summary>
		/// <remarks>Color contrast is calculated as how contrasting the 
		/// pixel is with regards to surrounding pixels.</remarks>
		public float MaxColorContrast
		{
			get { return this.maxColorContrast; }
			set { this.maxColorContrast = value; }
		}
		/// <summary>Maximum acceptable color distance between pixels.</summary>
		public float MaxColorDistance
		{
			get { return this.maxColorDistance; }
			set { this.maxColorDistance = value; }
		}
		/// <summary>Maximum acceptable proportion of pixels that may
		/// not meet the criteria (in relation to the master image) before 
		/// the operation is considered to have failed, typically ranging from 
		/// 0 to 1.</summary>
		public float MaxErrorProportion
		{
			get { return this.maxErrorProportion; }
			set { this.maxErrorProportion = value; }
		}
		/// <summary>Maximum pixel distance to search for acceptable values.</summary>
		public int MaxPixelDistance
		{
			get { return this.maxPixelDistance; }
			set { this.maxPixelDistance = value; }
		}
		/// <summary>Minimum acceptable color distance between pixels.</summary>
		public float MinColorDistance
		{
			get { return this.minColorDistance; }
			set { this.minColorDistance = value; }
		}
		/// <summary>Minimum acceptable color contrast between pixels.</summary>
		/// <remarks>Color contrast is calculated as how contrasting 
		/// the pixel is with regards to surrounding pixels.</remarks>
		public float MinColorContrast
		{
			get { return this.minColorContrast; }
			set { this.minColorContrast = value; }
		}
		/// <summary>Minimum acceptable brightness distance between pixels.</summary>
		public float MinBrightnessDistance
		{
			get { return this.minBrightnessDistance; }
			set { this.minBrightnessDistance = value; }
		}
		/// <summary>Minimum acceptable brightness constrast between pixels.</summary>
		/// <remarks>Brightness contrast is calculated as how contrasting 
		/// the pixel is with regards to surrounding pixels.</remarks>
		public float MinBrightnessContrast
		{
			get { return this.minBrightnessContrast; }
			set { this.minBrightnessContrast = value; }
		}
		/// <summary>Description of a mismatched criteria case. Purely
		/// informative, usually for diagnostic purposes.</summary>
		public string MismatchDescription
		{
			get { return (mismatchDescription == null)? String.Empty : mismatchDescription; }
			set { mismatchDescription = value; }
		}
		#endregion Public properties.

        /// <summary>Default comparison criteria, a perfect match between two 
        /// images</summary>
		public static ComparisonCriteria PerfectMatch = new ComparisonCriteria();

        /// <summary>Verifies whether two ComparisonCriteria objects are equal
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>true if the objects are equal, false otherwise.</returns>
		public override bool Equals(Object obj) 
		{
			ComparisonCriteria x = this;
			ComparisonCriteria y = obj as ComparisonCriteria;
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			return
				(x.MinColorDistance == y.MinColorDistance) &&
				(x.MinColorContrast == y.MinColorContrast) &&
				(x.MinBrightnessDistance == y.MinBrightnessDistance) &&
				(x.MinBrightnessContrast == y.MinBrightnessContrast) &&
				(x.MaxColorDistance == y.MaxColorDistance) &&
				(x.MaxColorContrast == y.MaxColorContrast) &&
				(x.MaxBrightnessDistance == y.MaxBrightnessDistance) &&
				(x.MaxBrightnessContrast == y.MaxBrightnessContrast) &&
				(x.MaxPixelDistance == y.MaxPixelDistance) &&
				(x.MaxErrorProportion == y.MaxErrorProportion) &&
				(x.MismatchDescription == y.MismatchDescription);
		}

        /// <summary>Returns a hash code for the class.</summary>
        /// <remarks>This is quite shoddy. It is just meant to base the
        /// hashing on values instead of identities, as required by the
        /// fact that the Equals operation is being overridden too.</remarks>
        /// <returns>A hash value.</returns>
		public override int GetHashCode() 
		{
			return 
				MinColorDistance.GetHashCode() 
				^ MinBrightnessDistance.GetHashCode()
				^ MaxColorDistance.GetHashCode()
				^ MaxPixelDistance;
		}
        
        /// <summary>Returns the value of the criteria as a string.</summary>
        /// <returns>The object as a string.</returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            sb.Append("Comparison criteria definition.");
            sb.Append("\r\nMinColorDistance: ");
            sb.Append(MinColorDistance.ToString());
            sb.Append("\r\nMinColorContrast: ");
            sb.Append(MinColorContrast.ToString());
            sb.Append("\r\nMinBrightnessDistance: ");
            sb.Append(MinBrightnessDistance.ToString());
            sb.Append("\r\nMinBrightnessContrast: ");
            sb.Append(MinBrightnessContrast.ToString());
            sb.Append("\r\nMaxColorDistance: ");
            sb.Append(MaxColorDistance.ToString());
            sb.Append("\r\nMaxColorContrast: ");
            sb.Append(MaxColorContrast.ToString());
            sb.Append("\r\nMaxBrightnessDistance: ");
            sb.Append(MaxBrightnessDistance.ToString());
            sb.Append("\r\nMaxBrightnessContrast: ");
            sb.Append(MaxBrightnessContrast.ToString());
			sb.Append("\r\nMaxPixelDistance: ");
			sb.Append(MaxPixelDistance.ToString());
			sb.Append("\r\nMaxErrorProportion: ");
			sb.Append(MaxErrorProportion.ToString());
            return sb.ToString();
        }
	}
}
