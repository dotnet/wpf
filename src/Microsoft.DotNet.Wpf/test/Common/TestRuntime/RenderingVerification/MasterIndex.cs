// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Drawing;
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public class MasterIndex
    {
        // Note : Only Tiff format allows us to store any kind of metadata we want within the image.
        // (Jpeg metadata is fairly flexible as well but the image encoding uses a loosy algo, unacceptable for vscan)
        private const string IMAGE_EXTENSION = ".tif";

        private string _fileName = string.Empty;
        private string _path = string.Empty;
        private string _resolvedMasterName = string.Empty;
        private Dictionary<IMasterDimension, int> _weightedCriteria = new Dictionary<IMasterDimension, int>();

        /// <summary>
        /// Get/set the master invariant Name
        /// </summary>
        public string FileName
        {
            get { return _fileName + "_*" + IMAGE_EXTENSION; }
            set
            {
                _fileName = value;
            }
        }
        /// <summary>
        /// Get/set the Path to master
        /// </summary>
        public string Path
        {
            get { return _path; }
            set
            {                
                _path = System.IO.Path.Combine(DriverState.TestBinRoot, value);
            }
        }
        internal string InvariantFileName
        {
            get { return _fileName; }
        }
        internal string ResolvedMasterName
        {
            get { return _resolvedMasterName; }
        }


        /// <summary>
        /// Create an instance of the MasterIndex class
        /// </summary>
        public MasterIndex()
        {
        }
        /// <summary>
        /// Create an instance of the MasterIndex class
        /// </summary>
        /// <param name="relativepath">The path where the masters live</param>
        /// <param name="invariantMasterName">The invariant name of the master</param>
        public MasterIndex(string relativepath, string invariantMasterName) : this()
        {
            _path = System.IO.Path.Combine(DriverState.TestBinRoot, relativepath);
            _fileName = invariantMasterName;
        }

        /// <summary>
        /// Add a criteria, it will be evaluated when seraching for a master
        /// </summary>
        /// <param name="dimension">The dimension to add</param>
        /// <param name="weight">The weight of the dimension</param>
        public void AddCriteria(IMasterDimension dimension, int weight)
        {
            MasterDimensionIndexableAttribute[] indexables = (MasterDimensionIndexableAttribute[])dimension.GetType().GetCustomAttributes(typeof(MasterDimensionIndexableAttribute), true);
            if (indexables.Length == 0 || indexables[0].IsIndexable == false) 
            { 
                throw new NotSupportedException("This type ('" + dimension.GetType().Name + "') cannot be used as criteria to pick a master( only used to describe the master).\r\nIf you need to change this, prepend the class with the [MasterIndexableAttribute(true)]"); 
            }
            
            _weightedCriteria.Add(dimension, weight);
        }

        internal Dictionary<IMasterDimension, string> GetCurrentCriteriaValue()
        {
            Dictionary<IMasterDimension, string> retVal = new Dictionary<IMasterDimension, string>();
            foreach (KeyValuePair<IMasterDimension, int> valuePair in _weightedCriteria)
            {
                retVal.Add(valuePair.Key, valuePair.Key.GetCurrentValue());
            }
            return retVal;
        }

        internal Bitmap Resolve()
        {
            _resolvedMasterName = string.Empty;
            if (System.IO.Directory.Exists(_path) == false) { return null; }

            // Get the current Dimensions (and its the values) for what the user cares about
            Dictionary<IMasterDimension, string> expectedDimensions = GetCurrentCriteriaValue();


            // Get all masters with for this testcase
            string[] files = System.IO.Directory.GetFiles(_path, FileName);

            // Loop thru all potential masters
            int[] matchingScores = new int[files.Length];
            for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                matchingScores[fileIndex] = 0;

                MasterMetadata masterMetadata = null;
                using (Image img = new Bitmap(files[fileIndex]))
                {
                    // Retrieve Image metadata (Description and Criteria) for each master
                    masterMetadata = ImageMetadata.MetadataFromImage(img);
                }

                // Check that every master dimensions are required by the user
                foreach (KeyValuePair<IMasterDimension, string> masterDim in masterMetadata.Criteria)
                {
                    if (expectedDimensions.ContainsKey(masterDim.Key) == false)
                    {
                        // User did not specfy this Dimension
                        matchingScores[fileIndex] = -1;
                        break;
                    }
                    if (expectedDimensions[masterDim.Key].ToLowerInvariant() != masterDim.Value.ToLowerInvariant())
                    {
                        // Dimension specified by user but this machine is not the right value.
                        matchingScores[fileIndex] = -1;
                        break;
                    }
                    matchingScores[fileIndex] += _weightedCriteria[masterDim.Key];
                }

            }

            // Find the best master  
            int bestMatchIndex = -1;
            int bestScore = -1;
            for (int t = 0; t < matchingScores.Length; t++)
            {
                if (matchingScores[t] > bestScore)
                {
                    bestScore = matchingScores[t];
                    bestMatchIndex = t;
                }
            }

            if (bestMatchIndex == -1) { return null; }

            _resolvedMasterName = files[bestMatchIndex];
            return new Bitmap(files[bestMatchIndex]);
        }

        internal string GetNewMasterName()
        { 
            int index = 0;
            string name = System.IO.Path.Combine(_path, _fileName + "_" +index + IMAGE_EXTENSION);
            while( System.IO.File.Exists(name) )    
            {
                index++;
                name = System.IO.Path.Combine(_path, _fileName + "_" + index + IMAGE_EXTENSION);
            }
            return System.IO.Path.GetFileName(name);
        }
    }
}
