// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Imaging
{
    #region Namespaces.

    using System;
    using System.Collections;
    using System.Drawing;

    using Microsoft.Test.Logging;

    #endregion Namespaces.

	/// <summary>
	/// This class is used to wrap simple image comparison operations.
	/// </summary>
	public class ComparisonOperationUtils
	{
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors.

        /// <summary>Hide the constructor for the utility class.</summary>
        private ComparisonOperationUtils() { }

        #endregion Constructors.

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public methods.

        /// <summary>
        /// Compares to bitmaps to determine whether they are equal.
        /// </summary>
        /// <param name='master'>Master image for comparison.</param>
        /// <param name='sample'>Sampled image for comparison.</param>
        /// <param name='differences'>
        /// On return, bitmap with differences highlighted if any were found,
        /// null otherwise.
        /// </param>
        /// <returns>true if master and sample are equal, false otherwise.</returns>
        public static bool AreBitmapsEqual(Bitmap master, Bitmap sample,
            out Bitmap differences)
        {
            string dummyDifferenceLog;
            return AreBitmapsEqual(master, sample, out differences, out dummyDifferenceLog);
        }

        /// <summary>
        /// Compares to bitmaps based on tolerance criteria
        /// </summary>
        /// <param name='sourceImage'>Master image for comparison.</param>
        /// <param name='targetImage'>Sampled image for comparison.</param>
        /// <param name='maxColorDistanceTolerance'>Maximum color distance tolerance</param>
        /// <returns>true if master and sample are equal, false otherwise.</returns>
        public static bool AreBitmapsEqualUsingCriteria(Bitmap sourceImage, Bitmap targetImage, float maxColorDistanceTolerance)
        {            
            ComparisonCriteria criteria = new ComparisonCriteria();
            criteria.MaxColorDistance = maxColorDistanceTolerance;
            
            return AreBitmapsEqualUsingCriteria(sourceImage, targetImage, criteria);
        }

        /// <summary>
        /// Compares to bitmaps based on tolerance criteria
        /// </summary>
        /// <param name='sourceImage'>Master image for comparison.</param>
        /// <param name='targetImage'>Sampled image for comparison.</param>
        /// <param name='criteria'>Criteria for image comparison.</param>
        /// <returns>true if master and sample are equal, false otherwise.</returns>
        public static bool AreBitmapsEqualUsingCriteria(Bitmap sourceImage, Bitmap targetImage, ComparisonCriteria criteria)
        {
            return AreBitmapsEqualUsingCriteria(sourceImage, targetImage, criteria, true);            
        }

        /// <summary>
        /// Compares to bitmaps based on tolerance criteria
        /// </summary>
        /// <param name='sourceImage'>Master image for comparison.</param>
        /// <param name='targetImage'>Sampled image for comparison.</param>
        /// <param name='criteria'>Criteria for image comparison.</param>
        /// <param name='logBitmaps'>Log bitmaps if comparison fails</param>
        /// <returns>true if master and sample are equal, false otherwise.</returns>
        public static bool AreBitmapsEqualUsingCriteria(Bitmap sourceImage, Bitmap targetImage, ComparisonCriteria criteria, bool logBitmaps)
        {
            Bitmap differenceImage;
            return AreBitmapsEqualUsingCriteria(sourceImage, targetImage, out differenceImage, criteria, logBitmaps);
        }

        /// <summary>
        /// Compares to bitmaps based on tolerance criteria
        /// </summary>
        /// <param name='sourceImage'>Master image for comparison.</param>
        /// <param name='targetImage'>Sampled image for comparison.</param>
        /// <param name='differenceImage'>On return, bitmap with differences highlighted 
        /// if any were found, null otherwise.</param>
        /// <param name='criteria'>Criteria for image comparison.</param>
        /// <param name='logBitmaps'>Log bitmaps if comparison fails</param>
        /// <returns>true if master and sample are equal, false otherwise.</returns>
        public static bool AreBitmapsEqualUsingCriteria(Bitmap sourceImage, Bitmap targetImage, out Bitmap differenceImage, ComparisonCriteria criteria, bool logBitmaps)
        {
            ComparisonOperation operation;
            ComparisonResult result;

            operation = new ComparisonOperation();
            operation.Criteria = criteria;
            operation.MasterImage = sourceImage;
            operation.SampleImage = targetImage;
            result = operation.Execute();
            differenceImage = null;
            if ((result.CriteriaMet == false))
            {                
                AreBitmapsEqual(sourceImage, targetImage, out differenceImage);
                result.HighlightDifferences(differenceImage);
                if (logBitmaps)
                {                    
                    GlobalLog.LogStatus("Logging Images: sourceImage targetImage differencesImage:\r\n");
                    GlobalLog.LogDebug(result.ToString());
                    LogImageOnDisk(sourceImage, "sourceImage" + _combinationIndex.ToString() + ".png");
                    LogImageOnDisk(targetImage, "targetImage" + _combinationIndex.ToString() + ".png");
                    LogImageOnDisk(differenceImage, "differencesImage" + _combinationIndex.ToString() + ".png");
                    _combinationIndex++;
                }
            }
            return result.CriteriaMet;
        }

        /// <summary>
        /// saves bitmaps on disk
        /// </summary>
        /// <param name='image'> image for saving.</param>
        /// <param name='name'> image name.</param>
        private static void LogImageOnDisk(System.Drawing.Image image, string name)
        {
            new System.Security.Permissions.FileIOPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            image.Save(name , System.Drawing.Imaging.ImageFormat.Png);
            GlobalLog.LogFile(name);
        }

        #endregion Public methods.

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Compares to bitmaps to determine whether they are equal.
        /// </summary>
        /// <param name='master'>Master image for comparison.</param>
        /// <param name='sample'>Sampled image for comparison.</param>
        /// <param name='differences'>
        /// On return, bitmap with differences highlighted if any were found,
        /// null otherwise.
        /// </param>
        /// <param name='differenceLog'>
        /// On return, text description of any differences that were found,
        /// string.empty otherwise.</param>
        /// <returns>true if master and sample are equal, false otherwise.</returns>
        internal static bool AreBitmapsEqual(Bitmap master, Bitmap sample,
            out Bitmap differences, out string differenceLog)
        {
            ComparisonOperation op = new ComparisonOperation();
            op.MasterImage = master;
            op.SampleImage = sample;

            ComparisonResult result = op.Execute();

            if (result.CriteriaMet)
            {
                differences = null;
                differenceLog = string.Empty;
                return true;
            }
            else
            {
                differences = new Bitmap(sample);
                result.HighlightDifferences(differences);
                // Use ToString rather than ToStringBrief - there will not be multiple
                // differences in a quick strict comparison.
                differenceLog = result.ToString();
                return false;
            }
        }

        #region private data.

        private static int _combinationIndex = 0;

        #endregion data.
    }
}
