// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description: An MSBuild task that classify the input files to different
//              categories based on the input item's attributes.
//
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;

using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;


using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using MS.Utility;
using MS.Internal.Tasks;

// Since we disable PreSharp warnings in this file, PreSharp warning is unknown to C# compiler.
// We first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace Microsoft.Build.Tasks.Windows
{
    /// <summary>
    /// The File Classification task puts all the input baml files, image files into different
    /// output resource groups, such as Resources for Main assembly and Resources for satellite
    /// assembly.
    /// </summary>
    public sealed class FileClassifier : Task
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor 
        /// </summary>
        public FileClassifier()
            : base(SR.SharedResourceManager)
        {
            // set default values for some non-required input items

            _culture = null;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// ITask Execute method
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            bool ret = false;
            var mainEmbeddedList = new List<ITaskItem>();
            var satelliteEmbeddedList = new List<ITaskItem>();
            var clrEmbeddedResourceList = new List<ITaskItem>();
            var clrSatelliteEmbeddedResourceList = new List<ITaskItem>();

            try
            {
                TaskHelper.DisplayLogo(Log, nameof(FileClassifier));

                ret = VerifyTaskInputs();

                if (ret != false)
                {
                    // Do the real work to classify input files.
                    Classify(SourceFiles, mainEmbeddedList, satelliteEmbeddedList);
                    
                    if (CLRResourceFiles != null)
                    {
                        // Generate the output CLR embedded resource list.
                        Classify(CLRResourceFiles, clrEmbeddedResourceList, clrSatelliteEmbeddedResourceList);
                    }

                    // move the arraylist to the TaskItem array.
                    MainEmbeddedFiles = mainEmbeddedList.ToArray();
                    SatelliteEmbeddedFiles = satelliteEmbeddedList.ToArray();
                    CLREmbeddedResource = clrEmbeddedResourceList.ToArray();
                    CLRSatelliteEmbeddedResource = clrSatelliteEmbeddedResourceList.ToArray();
                }
            }
            catch (Exception e)
            {
                // PreSharp Complaint 6500 - do not handle null-ref or SEH exceptions.
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
                else
                {
                    string message;
                    string errorId;

                    errorId = Log.ExtractMessageCode(e.Message, out message);

                    if (String.IsNullOrEmpty(errorId))
                    {
                        errorId = UnknownErrorID;
                        message = SR.Get(SRID.UnknownBuildError, message);
                    }

                    Log.LogError(null, errorId, null, null, 0, 0, 0, 0, message, null);
                }

                return false;
            }
#pragma warning disable 6500
            catch // Non-CLS compliant errors
            {
                Log.LogErrorWithCodeFromResources(SRID.NonClsError);
                return false;
            }
#pragma warning restore 6500


            return ret;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// SourceFiles: List of Items thatare to be classified
        /// </summary>
        [Required]
        public ITaskItem [] SourceFiles { get; set; }

        /// <summary>
        /// Can have values (exe, or dll)
        /// </summary>
        [Required]
        public string OutputType { get; set; }

        /// <summary>
        /// Culture of the build. Can be null if the build is non-localizable
        /// </summary>
        public string Culture
        {
            get { return _culture != null ? _culture.ToLower(CultureInfo.InvariantCulture) : null; }
            set { _culture = value; }
        }


        /// <summary>
        /// The CLR resource file list.
        /// In Project file, those files will be define by type CLRResource.
        /// such as:  <Item Type="CLRResources" Include=" ...." Loacalizable="false/true" />
        /// </summary>
        /// <value></value>
        public ITaskItem[] CLRResourceFiles { get; set; }

        /// <summary>
        /// Output Item list for the CLR resources that will be saved in
        /// the main assembly.
        /// </summary>
        /// <value></value>
        [Output]
        public ITaskItem[] CLREmbeddedResource { get; set; }

        /// <summary>
        /// Output Item list for the CLR resources that will be saved in
        /// the satellite assembly.
        /// </summary>
        /// <value></value>
        [Output]
        public ITaskItem[] CLRSatelliteEmbeddedResource { get; set; }

        /// <summary>
        /// MainEmbeddedFiles
        ///
        /// Non-localizable resources which will be embedded into the Main assembly.
        /// </summary>
        [Output]
        public ITaskItem [] MainEmbeddedFiles { get; set; }

        /// <summary>
        /// SatelliteEmbeddedFiles
        ///
        /// Localizable files which are embedded to the Satellite assembly for the
        /// culture which is set in Culture property..
        /// </summary>
        [Output]
        public ITaskItem [] SatelliteEmbeddedFiles { get; set; }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        //
        // Verify all the propety values set from project file.
        // If any input value is set wrongly, report appropriate build error
        // and return false.
        //
        private bool VerifyTaskInputs()
        {
            bool bValidInput = true;

            //
            // Verify different property values
            //
            // OutputType is marked as Required, it should never be null or emtpy.
            // otherwise, the task should have been failed before the code goes here.

            string targetType = OutputType.ToLowerInvariant( );
            switch (targetType)
            {
                case SharedStrings.Library :
                case SharedStrings.Module  :
                case SharedStrings.WinExe  :
                case SharedStrings.Exe     :
                    break;
                default :
                    Log.LogErrorWithCodeFromResources(SRID.TargetIsNotSupported, targetType);
                    bValidInput = false;
                    break;
            }

            // SourceFiles property is marked as Required.
            // MSBUILD Engine should have checked the setting for this property
            // so don't need to recheck here.

            if (TaskHelper.IsValidCultureName(Culture) == false)
            {
                Log.LogErrorWithCodeFromResources(SRID.InvalidCulture, Culture);
                bValidInput = false;
            }

            return bValidInput;
        }

        private void Classify(IEnumerable<ITaskItem> inputItems, List<ITaskItem> mainList, List<ITaskItem> satelliteList)
        {
            foreach (ITaskItem inputItem in inputItems)
            {
                ITaskItem outputItem = new TaskItem
                {
                    ItemSpec = inputItem.ItemSpec,
                };

                // Selectively copy metadata over.
                outputItem.SetMetadata(SharedStrings.Link, inputItem.GetMetadata(SharedStrings.Link));
                outputItem.SetMetadata(SharedStrings.LogicalName, inputItem.GetMetadata(SharedStrings.LogicalName));

                if (IsItemLocalizable(inputItem))
                {
                    satelliteList.Add(outputItem);
                }
                else
                {
                    mainList.Add(outputItem);
                }
            }
        }

        // <summary>
        // Check if the item is localizable or not.
        // </summary>
        // <param name="fileItem"></param>
        // <returns></returns>
        private bool IsItemLocalizable(ITaskItem fileItem)
        {
            bool isLocalizable = false;

            // if the default culture is not set, by default all
            // the items are not localizable.

            if (Culture != null && Culture.Equals("") == false)
            {
                string localizableString;

                // Default culture is set, by default the item is localizable
                // unless it is set as false in the Localizable attribute.

                isLocalizable = true;

                localizableString = fileItem.GetMetadata(SharedStrings.Localizable);

                if (localizableString != null && String.Compare(localizableString, "false", StringComparison.OrdinalIgnoreCase) ==0 )
                {
                    isLocalizable = false;
                }
            }

            return isLocalizable;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private string _culture;

        private const string UnknownErrorID = "FC1000";

        #endregion Private Fields

    }
}
