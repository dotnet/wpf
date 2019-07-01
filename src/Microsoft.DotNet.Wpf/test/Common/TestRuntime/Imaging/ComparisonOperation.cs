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
    /// This class is used to drive the image matching operations.
    /// </summary>
    public class ComparisonOperation
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors.

        /// <summary>Creates a new ComparisonOperation instance.</summary>
        public ComparisonOperation()
        {
            this.criteria = ComparisonCriteria.PerfectMatch;
            this.logImagesOnCriteriaUnmet = true;
        }

        #endregion Constructors.

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public methods.

        /// <summary>Executes the comparison operation.</summary>
        /// <returns>The results of the comparison operation.</returns>
        public ComparisonResult Execute()
        {
            System.Diagnostics.Debug.Assert(this.criteria != null);
            if (MasterImage == null)
                throw new InvalidOperationException("Cannot compare images with null master image");
            if (SampleImage == null)
                throw new InvalidOperationException("Cannot compare images with null sample image");

            ComparisonAlgorithm algorithm = SelectAlgorithm();
            algorithm.Criteria = Criteria;
            algorithm.MasterImage = MasterImage;
            algorithm.SampleImage = SampleImage;

            ComparisonResult result = algorithm.Execute();
            result.FinishedComparison();

            return result;
        }

        /// <summary>
        /// Executes the comparison operation with supporting services.
        /// </summary>
        /// <param name="prefixName">
        /// Prefix name for criteria and operation configuration arguments.
        /// </param>
        /// <returns>The results of the comparison operation.</returns>
        /// <remarks><p>
        /// This method implements the typical wrapping around
        /// the comparison API, where services are available to help
        /// with configuration and logging.
        /// </p><p>
        /// The operation is configured, and a new criteria object is
        /// configured and assigned unless the current criteria has
        /// already been modified in some way. If the criteria is not
        /// met, an exception is thrown. If LogImagesOnUnmet is true and
        /// the criteria is not met, the images will be logged.
        /// </p><p>
        /// The ComparisonOperation object and the ComparisonResult
        /// object are configured with the
        /// ConfigurationSettings.SetObjectProperties method; see
        /// this API for more information.
        /// </p></remarks>
        public ComparisonResult ExecuteServiced(string prefixName)
        {
#if (IGNORE_IMAGE_LOGGING)
            throw new NotImplementedException("ComparisonOperation.ExecuteServiced is not implemented when IGNORE_IMAGE_LOGGING is defined.");
#else
            if (prefixName == null)
            {
                throw new ArgumentNullException("prefixName");
            }
            GlobalLog.LogStatus("Performing {" + prefixName + "} comparison...");            

            if (Criteria.Equals(ComparisonCriteria.PerfectMatch))
            {
                // Doing a SetObjectProperties call on this.criteria
                // would modify the properties of the shared perfect match
                // instance.
                ComparisonCriteria newCriteria = new ComparisonCriteria();
                //ConfigurationSettings.Current
                //    .SetObjectProperties(newCriteria, prefixName);
                Criteria = newCriteria;
                GlobalLog.LogStatus(criteria.ToString());
            }

            ComparisonResult result = Execute();

            if (!result.CriteriaMet)
            {
                if (LogImagesOnCriteriaUnmet)
                {
                    masterImage.Save(prefixName + "master" , System.Drawing.Imaging.ImageFormat.Png);
                    GlobalLog.LogFile(prefixName + "master");
                    //l.LogImage(masterImage, prefixName + "master");
                    sampleImage.Save(prefixName + "sample", System.Drawing.Imaging.ImageFormat.Png);
                    GlobalLog.LogFile(prefixName + "sample");

                    //l.LogImage(sampleImage, prefixName + "sample");
                    Bitmap differences = new Bitmap(sampleImage);
                    result.HighlightDifferences(differences);

                    differences.Save(prefixName + "differences", System.Drawing.Imaging.ImageFormat.Png);
                    GlobalLog.LogFile(prefixName + "differences");
                    //l.LogImage(differences, prefixName + "differences");
                }
                
                string message = criteria.MismatchDescription +
                    Environment.NewLine + result.ToString();
                throw new Exception(message);
            }
            else
            {
                GlobalLog.LogStatus(result.ToStringBrief());
            }
            return result;
#endif
        }

        #endregion Public methods.

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public properties.

        /// <summary>Criteria to be used in comparison.</summary>
        public ComparisonCriteria Criteria
        {
            get { return this.criteria; }
            set { this.criteria = (value == null)? ComparisonCriteria.PerfectMatch : value; }
        }

        /// <summary>
        /// Whether to log images when the comparison criteria is
        /// unmet during a serviced execution.
        /// </summary>
        public bool LogImagesOnCriteriaUnmet
        {
            get { return this.logImagesOnCriteriaUnmet; }
            set { this.logImagesOnCriteriaUnmet = value; }
        }

        /// <summary>Master image for comparison.</summary>
        public Bitmap MasterImage
        {
            get { return this.masterImage; }
            set { this.masterImage = value; }
        }

        /// <summary>Sampled image for comparison.</summary>
        public Bitmap SampleImage
        {
            get { return this.sampleImage; }
            set { this.sampleImage = value; }
        }

        #endregion Public properties.

        /// <summary>
        /// Selects the algorithm that performs best for the selected
        /// criteria.
        /// </summary>
        /// <returns>The best comparison algorithm.</returns>
        private ComparisonAlgorithm SelectAlgorithm()
        {
            System.Diagnostics.Debug.Assert(this.criteria != null);
            if (Criteria.Equals(ComparisonCriteria.PerfectMatch))
            {
                return new PerfectMatchAlgorithm();
            }
            bool accountForDistance = Criteria.MaxPixelDistance > 0;
            bool accountForContrast =
                (Criteria.MaxBrightnessContrast > 0) ||
                (Criteria.MinBrightnessContrast > 0) ||
                (Criteria.MaxColorContrast > 0) ||
                (Criteria.MinColorContrast > 0);
            bool accountForBrightness =
                (Criteria.MaxBrightnessContrast > 0) ||
                (Criteria.MinBrightnessContrast > 0);
            if (accountForContrast)
            {
                throw new NotImplementedException("Contrast calculation is not implemented.");
            }
            else
            {
                if (accountForBrightness)
                {
                    throw new NotImplementedException("Brightness calculation is not implemented.");
                }
                else
                {
                    return new ColorComparisonAlgorithm();
                }
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private fields.

        private ComparisonCriteria criteria;
        private bool logImagesOnCriteriaUnmet;
        private Bitmap masterImage;
        private Bitmap sampleImage;

        #endregion Private fields.

    }
}
