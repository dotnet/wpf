// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description: An MSBuild task that generate .resources file from given
//              resource files, .jpg, .ico, .baml, etc.
//
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;

using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using MS.Utility;
using Microsoft.Build.Tasks.Windows;
using MS.Internal;
using MS.Internal.Tasks;

// Since we disable PreSharp warnings in this file, PreSharp warning is unknown to C# compiler.
// We first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace Microsoft.Build.Tasks.Windows
{
    public sealed class ResourcesGenerator : Task
    {
        // We want to avoid holding file handles for arbitrary lengths of time, so we pass instances of this class
        // to ResourceWriter which only opens the handle when it's being used.
        // We use flags in ResourceWriter so that it will opportunistically dispose the stream.
        // This has potential for delaying failures, but that's acceptable in this scenario.
        private class LazyFileStream : Stream
        {
            private readonly string _sourcePath;
            private FileStream _sourceStream;

            public LazyFileStream(string path)
            {
                _sourcePath = Path.GetFullPath(path);
            }

            private Stream SourceStream
            {
                get
                {
                    if (_sourceStream == null)
                    {
                        _sourceStream = new FileStream(_sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                        // limit size to System.Int32.MaxValue
                        long length = _sourceStream.Length;
                        if (length > (long)System.Int32.MaxValue)
                        {
                            throw new ApplicationException(SR.Get(SRID.ResourceTooBig, _sourcePath, System.Int32.MaxValue));
                        }

                    }
                    return _sourceStream;
                }
            }

            public override bool CanRead { get { return true; } }

            public override bool CanSeek { get { return true; } }

            public override bool CanWrite { get { return false; } }

            public override void Flush() {}

            public override long Length
            {
                get { return SourceStream.Length; }
            }

            public override long Position
            {
                get { return SourceStream.Position; }
                set { SourceStream.Position = value; }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return SourceStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return SourceStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                // This is backed by a readonly file.
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                // This is backed by a readonly file.
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (null != _sourceStream)
                    {
                        _sourceStream.Dispose();
                        _sourceStream = null;
                    }
                }
            }
        }
        
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        public ResourcesGenerator ( )
                 : base(SR.SharedResourceManager)
        {
            // set the source directory
            SourceDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        public override bool Execute()
        {
            TaskHelper.DisplayLogo(Log, nameof(ResourcesGenerator));

            //
            // Validate the property settings
            //

            if (ValidResourceFiles(ResourceFiles) == false)
            {
               // ValidResourceFiles has already showed up error message.
               // Just stop here.
               return false;
            }

            if (OutputResourcesFile != null && OutputResourcesFile.Length > 1)
            {
                // Every task should generate only one .resources.
                Log.LogErrorWithCodeFromResources(SRID.MoreResourcesFiles);
                return false;
            }

            try
            {
                // create output directory
                if (!Directory.Exists(OutputPath))
                {
                    Directory.CreateDirectory(OutputPath);
                }

                string resourcesFile = OutputResourcesFile[0].ItemSpec;

                Log.LogMessageFromResources(MessageImportance.Low, SRID.ResourcesGenerating, resourcesFile);

                // Go through all the files and create a resources file.
                using (var resWriter = new ResourceWriter(resourcesFile))
                {
                    foreach (var resourceFile in ResourceFiles)
                    {
                        string resFileName = resourceFile.ItemSpec;
                        string resourceId = GetResourceIdForResourceFile(resourceFile);

                        // We're handing off lifetime management for the stream.
                        // True for the third argument tells resWriter to dispose of the stream when it's done.
                        resWriter.AddResource(resourceId, new LazyFileStream(resFileName), true);

                        Log.LogMessageFromResources(MessageImportance.Low, SRID.ReadResourceFile, resFileName);
                        Log.LogMessageFromResources(MessageImportance.Low, SRID.ResourceId, resourceId);

                    }
                    // Generate the .resources file.
                    resWriter.Generate();
                }

                Log.LogMessageFromResources(MessageImportance.Low, SRID.ResourcesGenerated, resourcesFile);
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
                    string errorId = Log.ExtractMessageCode(e.Message, out message);

                    if (string.IsNullOrEmpty(errorId))
                    {
                        errorId = UnknownErrorID;
                        message = SR.Get(SRID.UnknownBuildError, message);
                    }

                    Log.LogError(null, errorId, null, null, 0, 0, 0, 0, message, null);
                    return false;
                }
            }
#pragma warning disable 6500
            catch   // Non-cls compliant errors
            {
                Log.LogErrorWithCodeFromResources(SRID.NonClsError);
                return false;
            }
#pragma warning restore 6500

            return true;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        ///<summary>
        /// Image or baml files which will be embedded into Resources File
        ///</summary>
        [Required]
        public ITaskItem [] ResourceFiles { get; set; }

        ///<summary>
        /// The directory where the generated resources file lives.
        ///</summary>
        [Required]
        public string OutputPath
        {
            get { return _outputPath; }
            set
            {
                _outputPath = Path.GetFullPath(value);

                if (!_outputPath.EndsWith((Path.DirectorySeparatorChar).ToString(), StringComparison.Ordinal))
                    _outputPath += Path.DirectorySeparatorChar;
            }
        }

        ///<summary>
        /// Generated resources file. This is a required input item.
        /// Every task should generate only one .resource file.
        /// If the resource is for a specific culture, the ItemSpec should have syntax like
        ///      Filebasename.$(Culture).resources.
        ///
        /// The file path should also include OutputPath.
        ///</summary>
        [Output]
        [Required]
        public ITaskItem [] OutputResourcesFile { get; set; }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Check if the passed files have valid path
        /// </summary>
        /// <param name="inputFiles"></param>
        /// <returns></returns>
        private bool ValidResourceFiles(ITaskItem[] inputFiles)
        {
            bool bValid = true;

            foreach (ITaskItem  inputFile  in inputFiles)
            {
                string strFileName;

                strFileName = inputFile.ItemSpec;

                if (!File.Exists(TaskHelper.CreateFullFilePath(strFileName, SourceDir)))
                {
                    bValid = false;
                    Log.LogErrorWithCodeFromResources(SRID.FileNotFound, strFileName);
                }
            }

            return bValid;
        }


        /// <summary>
        /// Return the correct resource id for the passed resource file path.
        /// </summary>
        /// <param name="resFile">Resource File Path</param>
        /// <returns>the resource id</returns>
        private string GetResourceIdForResourceFile(ITaskItem resFile)
        {
            bool requestExtensionChange = true;
            
            return GetResourceIdForResourceFile(
                resFile.ItemSpec, 
                resFile.GetMetadata(SharedStrings.Link), 
                resFile.GetMetadata(SharedStrings.LogicalName),
                OutputPath,
                SourceDir,
                requestExtensionChange);
        }

        internal static string GetResourceIdForResourceFile(
            string filePath, 
            string linkAlias, 
            string logicalName,
            string outputPath,
            string sourceDir,
            bool   requestExtensionChange)
        {
            string relPath = String.Empty;

            // Please note the subtle distinction between <Link /> and <LogicalName />. 
            // <Link /> is treated as a fully resolvable path and is put through the same 
            // transformations as the original file path. <LogicalName /> on the other hand 
            // is treated as an alias for the given resource and is used as is. Whether <Link /> 
            // was meant to be treated thus is debatable. Nevertheless in .Net 4.5 it would 
            // amount to a breaking change to have to change the behavior of <Link /> and 
            // hence the choice to support <LogicalName /> with the desired semantics. All 
            // said in most of the regular scenarios using <Link /> or <Logical /> will result in 
            // the same resourceId being picked.

            if (!String.IsNullOrEmpty(logicalName))
            {
                // Use the LogicalName when there is one
                logicalName = ReplaceXAMLWithBAML(filePath, logicalName, requestExtensionChange);
                relPath = logicalName;
            }
            else
            {
                // Always use the Link tag if it's specified.
                // This is the way the resource appears in the project.
                linkAlias = ReplaceXAMLWithBAML(filePath, linkAlias, requestExtensionChange);
                filePath = !string.IsNullOrEmpty(linkAlias) ? linkAlias : filePath;
                string fullFilePath = Path.GetFullPath(filePath);
                
                //
                // If the resFile, or it's perceived path, is relative to the StagingDir
                // (OutputPath here) take the relative path as resource id.
                // If the resFile is not relative to StagingDir, but relative
                // to the project directory, take this relative path as resource id.
                // Otherwise, just take the file name as resource id.
                //
                
                relPath = TaskHelper.GetRootRelativePath(outputPath, fullFilePath);
                
                if (string.IsNullOrEmpty(relPath))
                {
                    relPath = TaskHelper.GetRootRelativePath(sourceDir, fullFilePath);
                }
                
                if (string.IsNullOrEmpty(relPath))
                {
                    relPath = Path.GetFileName(fullFilePath);
                }
            }

            // Modify resource ID to correspond to canonicalized Uri format
            // i.e. - all lower case, use "/" as separator
            // ' ' is converted to escaped version %20
            //

            string resourceId = ResourceIDHelper.GetResourceIDFromRelativePath(relPath);

            return resourceId;
        }

        private static string ReplaceXAMLWithBAML(string sourceFilePath, string path, bool requestExtensionChange)
        {
            if (requestExtensionChange && 
                Path.GetExtension(sourceFilePath).Equals(SharedStrings.BamlExtension) && 
                Path.GetExtension(path).Equals(SharedStrings.XamlExtension))
            {
                // Replace the path extension to baml only if the source file path is baml
                path = Path.ChangeExtension(path, SharedStrings.BamlExtension);
            }

            return path;
        }

        #endregion Private Methods
        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private string SourceDir { get; set; }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private string                     _outputPath;

        private const string UnknownErrorID = "RG1000";

        #endregion Private Fields

    }
}

