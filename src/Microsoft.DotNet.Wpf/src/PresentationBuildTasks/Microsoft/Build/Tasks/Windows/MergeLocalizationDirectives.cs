// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description: The task that merges all the localization directives files
//
//---------------------------------------------------------------------------


using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Runtime.InteropServices;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using MS.Internal.Globalization;
using MS.Internal.Tasks;
using MS.Utility;                   // For SR

// Since we disable PreSharp warnings in this file, we first need to disable warnings
// about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace Microsoft.Build.Tasks.Windows
{
    /// <summary>
    /// This task merges the localization directives files of single bamls
    /// into one file corresponding to for the compiled assembly.
    /// </summary>
    public sealed class MergeLocalizationDirectives : Task
    {
        //--------------------------------
        // Constructor
        //--------------------------------
        /// <summary>
        /// Default constructor of the task
        /// </summary>
        public MergeLocalizationDirectives() : base(SR.SharedResourceManager)
        {
        }

        //--------------------------------
        // Public methods
        //--------------------------------

        /// <summary>
        /// Method invoked by MSBuild to merge localization files of single bamls to
        /// one file for the whole Assembly.
        /// </summary>
        public override bool Execute()
        {
            TaskHelper.DisplayLogo(Log, nameof(MergeLocalizationDirectives));
            if (GeneratedLocalizationFiles.Length > 0)
            {
                try {

                    string absoluteFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        _outputFile
                        );

                    using (StreamWriter streamWriter = new StreamWriter(
                              new FileStream(absoluteFilePath, FileMode.Create),
                              new UTF8Encoding(true)
                              )
                          )
                    {

                        Log.LogMessageFromResources(SRID.CommentFileGenerating, _outputFile);

                        streamWriter.WriteLine("<" + LocComments.LocDocumentRoot + ">");

                        // keep things simple and fast. Just keep appending the
                        // xml fragments that are already outputed.
                        foreach (ITaskItem item in GeneratedLocalizationFiles)
                        {
                            using (StreamReader locStreamReader = new StreamReader(item.ItemSpec))
                            {
                                // directly concat Xml fragments
                                streamWriter.WriteLine(locStreamReader.ReadToEnd());
                            }
                        }

                        streamWriter.WriteLine("</" + LocComments.LocDocumentRoot + ">");
                        Log.LogMessageFromResources(SRID.CommentFileGenerated, _outputFile);
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
                        Log.LogErrorFromException(e);
                        return false;
                    }
                }
#pragma warning disable 6500
                catch // Non-CLS compliant errors
                {
                    Log.LogErrorWithCodeFromResources(SRID.NonClsError);
                    return false;
                }
#pragma warning restore 6500
            }

            return true;
        }

        //--------------------------------
        // Public properties
        //--------------------------------

        /// <summary>
        /// The list of localization directives files for individual Bamls.
        /// </summary>
        [Required]
        public ITaskItem[] GeneratedLocalizationFiles
        {
            get { return _generatedLocalizationFiles; }
            set { _generatedLocalizationFiles = value; }
        }

        /// <summary>
        /// The output path of the compiled assembly
        /// </summary>
        [Required]
        [Output]
        public string OutputFile
        {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        //---------------------------------
        // Private members
        //---------------------------------
        private ITaskItem[] _generatedLocalizationFiles;
        private string      _outputFile;
    }
}

