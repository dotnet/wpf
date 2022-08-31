// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//----------------------------------------------------------------------------------------
//
// Description:
//       A wrapper class which can take inputs from build task and call the underneath
//       MarkupCompiler, CompilationUnit and other related types to do the real
//       compilation work.
//       This wrapper runs in the same Appdomain as MarkupCompiler, CompilationUnit,
//
//       It miminizes the communication between two domains among the build tasks and
//       the real compiler classes.
//
//---------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

using MS.Internal.Markup;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using MS.Internal.Tasks;
using MS.Utility;

namespace MS.Internal
{
    // <summary>
    // CompilerWrapper
    // </summary>
    internal class CompilerWrapper : MarshalByRefObject
    {
        // <summary>
        // ctor
        // </summary>
        internal CompilerWrapper()
        {
            _mc = new MarkupCompiler();
            _sourceDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
            _nErrors = 0;
        }

        // <summary>
        // The valid source file extension for the passed language.
        // Normally a language supports more valid source file extensions.
        // User could choose one of them in project file.
        // If this property is not set, we will take use of the default one for the language.
        // </summary>
        internal string LanguageSourceExtension
        {
            set { _mc.LanguageSourceExtension = value; }
        }

        //<summary>
        // OutputPath : Generated code files, Baml fles will be put in this directory.
        //</summary>
        internal string OutputPath
        {
            set { _mc.TargetPath = value; }
        }

        // <summary>
        // The version of the assembly
        // </summary>
        internal string AssemblyVersion
        {
            set { _mc.AssemblyVersion = value; }
        }

        // <summary>
        // The key token of the assembly
        // </summary>
        internal string AssemblyPublicKeyToken
        {
            set { _mc.AssemblyPublicKeyToken = value; }
        }

        //<summary>
        // ApplicationMarkup
        //</summary>
        internal FileUnit ApplicationMarkup
        {
            set { _applicationMarkup = value; }
        }

        // <summary>
        // Loose file content list
        // </summary>
        internal string[] ContentFiles
        {
            set { _mc.ContentList = value; }
        }

        /// <summary>
        /// Splash screen image to be displayed before application init
        /// </summary>
        internal string SplashImage
        {
            set { _mc.SplashImage = value; }
        }

        // <summary>
        // Assembly References.
        // </summary>
        // <value></value>
        internal ArrayList References
        {
            set { _mc.ReferenceAssemblyList = value; }
        }

        //<summary>
        //If true code for supporting hosting in Browser is generated
        //</summary>
        internal bool HostInBrowser
        {
            set { _mc.HostInBrowser = value; }
        }

        //<summary>
        // Generated debugging information in the BAML file.
        //</summary>
        internal bool XamlDebuggingInformation
        {
            set { _mc.XamlDebuggingInformation = value; }
        }

        // <summary>
        // Controls how to generate localization information for each xaml file.
        // Valid values: None, CommentsOnly, All.
        //
        // </summary>
        internal int LocalizationDirectivesToLocFile
        {
            set { _localizationDirectivesToLocFile = value; }
        }

        // <summary>
        // Keep the application definition file if it contains local types which are implemented
        // in current target assembly.
        // </summary>
        internal string LocalXamlApplication
        {
            get { return _mc.LocalXamlApplication; }
        }


        // <summary>
        // Keep the page markup files if they contain local types which are implemented
        // in current target assembly.
        // </summary>
        internal string[] LocalXamlPages
        {
            get { return _mc.LocalXamlPages; }
        }


        /// <summary>
        /// Get/Sets the TaskFileService on MarkupCompiler
        /// </summary>
        internal ITaskFileService TaskFileService
        {
            get { return _mc.TaskFileService; }
            set { _mc.TaskFileService = value; }
        }

        //
        // A wrapper property which maps to MarkupCompiler class's corresponding property.
        // This property will be called by MarkupCompilePass1 and Pass2 build tasks.
        //
        // It indicates whether any xaml file in current xaml list references internal types
        // from friend assembly or local target assembly.
        //
        internal bool HasInternals
        {
            get { return MarkupCompiler.HasInternals; }
        }

        // <summary>
        // TaskLoggingHelper
        // </summary>
        internal TaskLoggingHelper TaskLogger
        {
            set
            {
                _taskLogger = value;
                _mc.TaskLogger = value;
            }
        }

        internal string UnknownErrorID
        {
            set { _unknownErrorID = value; }
        }

        internal int ErrorTimes
        {
            get { return _nErrors; }
        }

        internal bool SupportCustomOutputPaths 
        {
            set { _mc.SupportCustomOutputPaths = value; }
        }

        // <summary>
        // Start the compilation.
        // </summary>
        // <param name="assemblyName"></param>
        // <param name="language"></param>
        // <param name="rootNamespace"></param>
        // <param name="fileList"></param>
        // <param name="isSecondPass"></param>
        // <returns></returns>
        internal bool DoCompilation(string assemblyName, string language, string rootNamespace, FileUnit[] fileList, bool isSecondPass)
        {
            bool ret = true;

            CompilationUnit compUnit = new CompilationUnit(assemblyName, language, rootNamespace, fileList);
            compUnit.Pass2 = isSecondPass;

            // Set some properties required by the CompilationUnit
            compUnit.ApplicationFile = _applicationMarkup;
            compUnit.SourcePath = _sourceDir;

            //Set the properties required by MarkupCompiler

            _mc.SourceFileResolve += new SourceFileResolveEventHandler(OnSourceFileResolve);
            _mc.Error += new MarkupErrorEventHandler(OnCompilerError);

            LocalizationDirectivesToLocFile localizeFlag = (LocalizationDirectivesToLocFile)_localizationDirectivesToLocFile;


            //
            // Localization file should not be generated for Intellisense build. Thus
            // checking IsRealBuild.
            //
            if ((localizeFlag == MS.Internal.LocalizationDirectivesToLocFile.All
                 || localizeFlag == MS.Internal.LocalizationDirectivesToLocFile.CommentsOnly)
                && (TaskFileService.IsRealBuild))
            {
                _mc.ParserHooks = new LocalizationParserHooks(_mc, localizeFlag, isSecondPass);
            }

            if (isSecondPass)
            {
                for (int i = 0; i < _mc.ReferenceAssemblyList.Count; i++)
                {
                    ReferenceAssembly asmReference = _mc.ReferenceAssemblyList[i] as ReferenceAssembly;

                    if (asmReference != null)
                    {
                        if (String.Compare(asmReference.AssemblyName, assemblyName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Set the local assembly file to markupCompiler
                            _mc.LocalAssemblyFile = asmReference;
                        }
                    }
                }
            }

            // finally compile the app
            _mc.Compile(compUnit);

            return ret;
        }

        #region private method


        // <summary>
        // Event handler for the Compiler Errors
        // </summary>
        // <param name="sender"></param>
        // <param name="e"></param>
        private void OnCompilerError(Object sender, MarkupErrorEventArgs e)
        {
            _nErrors++;

            //
            // Since Output from LogError() cannot be recognized by VS TaskList, so
            // we replaced it with LogErrorFromText( ) and pass all the required information
            // such as filename, line, offset, etc.
            //
            string strErrorCode;

            // Generate error message by going through the whole exception chain, including
            // its inner exceptions.
            string message = TaskHelper.GetWholeExceptionMessage(e.Exception);
            string errorText;

            strErrorCode = _taskLogger.ExtractMessageCode(message, out errorText);

            if (String.IsNullOrEmpty(strErrorCode))
            {
                // If the exception is a Xml exception, show a pre-asigned error id for it.
                if (IsXmlException(e.Exception))
                {
                    message = SR.Get(SRID.InvalidXml, message);
                    strErrorCode = _taskLogger.ExtractMessageCode(message, out errorText);
                }
                else
                {
                    strErrorCode = _unknownErrorID;
                    errorText = SR.Get(SRID.UnknownBuildError, errorText);
                }
            }

            _taskLogger.LogError(null, strErrorCode, null, e.FileName, e.LineNumber, e.LinePosition, 0, 0, errorText);
        }


        //
        // Detect if the exception is xmlException
        //
        private bool IsXmlException(Exception e)
        {
            bool isXmlException = false;

            while (e != null)
            {
                if (e is System.Xml.XmlException)
                {
                    isXmlException = true;
                    break;
                }
                else
                {
                    e = e.InnerException;
                }
            }

            return isXmlException;

        }

        //
        // Handle the SourceFileResolve Event from MarkupCompiler.
        // It tries to GetResolvedFilePath for the new SourceDir and new RelativePath.
        //
        private void OnSourceFileResolve(Object sender, SourceFileResolveEventArgs e)
        {
            SourceFileInfo sourceFileInfo = e.SourceFileInfo;
            string newSourceDir = _sourceDir;
            string newRelativeFilePath;

            if (String.IsNullOrEmpty(sourceFileInfo.OriginalFilePath))
            {
                newRelativeFilePath = sourceFileInfo.OriginalFilePath;
            }
            else
            {
                newRelativeFilePath = GetResolvedFilePath(sourceFileInfo.OriginalFilePath, ref newSourceDir);

                _taskLogger.LogMessageFromResources(MessageImportance.Low, SRID.FileResolved, sourceFileInfo.OriginalFilePath, newRelativeFilePath, newSourceDir);
            }

            if (sourceFileInfo.IsXamlFile)
            {
                //
                // For Xaml Source file, we need to remove the .xaml extension part.
                //
                int fileExtIndex = newRelativeFilePath.LastIndexOf(MarkupCompiler.DOTCHAR);
                newRelativeFilePath = newRelativeFilePath.Substring(0, fileExtIndex);
            }

            //
            // Update the SourcePath and RelativeSourceFilePath property in SourceFileInfo object.
            //
            sourceFileInfo.SourcePath = newSourceDir;
            sourceFileInfo.RelativeSourceFilePath = newRelativeFilePath;

            // Put the stream here.
            sourceFileInfo.Stream = TaskFileService.GetContent(sourceFileInfo.OriginalFilePath);
        }

        //
        // Return a new sourceDir and relative filepath for a given filePath.
        // This is for supporting of fullpath or ..\ in the original FilePath.
        //
        private string GetResolvedFilePath(string filePath, ref string newSourceDir)
        {
            // make it an absolute path if not already so
            if (!Path.IsPathRooted(filePath))
            {
                filePath = _sourceDir + filePath;
            }

            // get rid of '..' and '.' if any
            string fullFilePath = Path.GetFullPath(filePath);

            // Get the relative path based on sourceDir
            string relPath = String.Empty;
            string newRelativeFilePath;

            if (fullFilePath.StartsWith(_sourceDir,StringComparison.OrdinalIgnoreCase))
            {
                relPath = fullFilePath.Substring(_sourceDir.Length);

                // the original file is relative to the SourceDir.
                newSourceDir = _sourceDir;
                newRelativeFilePath = relPath;
            }
            else
            {
                // the original file is not relative to the SourceDir.
                // it could have its own fullpath or contains "..\" etc.
                //
                // In this case, we want to put the filename as relative filepath
                // and put the deepest directory that file is in as the new
                // SourceDir.
                //
                int pathEndIndex = fullFilePath.LastIndexOf(Path.DirectorySeparatorChar);

                newSourceDir = fullFilePath.Substring(0, pathEndIndex + 1);
                newRelativeFilePath = fullFilePath.Substring(pathEndIndex + 1);
            }

            return newRelativeFilePath;
        }

        #endregion

        #region private data

        private MarkupCompiler _mc;
        private string         _sourceDir;
        private TaskLoggingHelper _taskLogger;
        private int    _nErrors;
        private string _unknownErrorID;

        private FileUnit _applicationMarkup;

        private int      _localizationDirectivesToLocFile;

        #endregion

    }
}
