// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description: An MSBuild Task that can generate .xaml markup file to specific
//              Mangaged language code such as .cs, .js, .vb,etc, and /or binary
//              token file .baml
//
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Security;

using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

using System.CodeDom;
using System.CodeDom.Compiler;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using MS.Utility;
using MS.Internal;
using MS.Internal.Tasks;
using MS.Internal.Markup;

// Since we disable PreSharp warnings in this file, PreSharp warning is unknown to C# compiler.
// We first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace Microsoft.Build.Tasks.Windows
{

    #region MarkupCompilePass1 Task class

    /// <summary>
    /// Class of MarkupCompilePass1 Task
    /// </summary>
    public sealed class MarkupCompilePass1 : Task
    {
        //  Security Concerns:
        //
        //  1) OutputPath property exposes the current dir and is publicly available.
        //  2) This class generates code files and copies them on the disk.
        //
        //  The above two are already mitigated by the following facts:
        //
        //  1) PresentationBuildTasks is not APTCA and thus partial trust assemblies
        //     cannot call into it.
        //  2) There is a possibility that some APTCA assembly (eg. PF.dll) can create
        //     this object and pass it on to partial trust code.  This is mitigated by
        //     FxCop rule against it.
        //

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public MarkupCompilePass1( ) : base(SR.SharedResourceManager)
        {
            // set the source directory
            _sourceDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;

            _outputType = SharedStrings.WinExe;

            // By default, no localization information is stripped out.
            _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.None;

            _nErrors = 0;

        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Execute method in Task
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            TaskHelper.DisplayLogo(Log, nameof(MarkupCompilePass1));

            bool bSuccess = true;

            try
            {

                //
                // Create the TaskFileService instance here
                //

                _taskFileService = new TaskFileService(this) as ITaskFileService;

                _compilerState = new CompilerState(
                    OutputPath + AssemblyName + (TaskFileService.IsRealBuild? SharedStrings.StateFile : SharedStrings.IntellisenseStateFile),
                    TaskFileService);

                _compilerLocalRefCache = new CompilerLocalReference(
                    OutputPath + AssemblyName + (TaskFileService.IsRealBuild? SharedStrings.LocalTypeCacheFile : SharedStrings.IntellisenseLocalTypeCacheFile),
                    TaskFileService);

                if ((PageMarkup == null || PageMarkup.Length == 0) &&
                    (ApplicationMarkup == null || ApplicationMarkup.Length == 0))
                {
                    // Don't need to do further work.
                    // stop here.
                    CleanupCacheFiles();
                    return true;
                }

                VerifyInputs();

                Log.LogMessageFromResources(MessageImportance.Low, SRID.CurrentDirectory, SourceDir);

                // If wrong files are set to some properties, the task
                // should stop here immediatelly.

                if (_nErrors > 0)
                {
                    Log.LogErrorWithCodeFromResources(SRID.WrongPropertySetting);
                }
                else
                {

                    // create output directory
                    if (!Directory.Exists(OutputPath))
                    {
                        Directory.CreateDirectory(OutputPath);
                    }

                    // Analyze project inputs to detect which xaml files require to recompile.
                    AnalyzeInputsAndSetting();

                    Log.LogMessageFromResources(MessageImportance.Low, SRID.AnalysisResult, CompilerAnalyzer.AnalyzeResult);

                    if (!SkipMarkupCompilation)
                    {

                        if (CompilerAnalyzer.RecompileMarkupPages != null)
                        {
                            for (int i = 0; i < CompilerAnalyzer.RecompileMarkupPages.Length; i++)
                            {

                                Log.LogMessageFromResources(MessageImportance.Low, SRID.RecompiledXaml, CompilerAnalyzer.RecompileMarkupPages[i]);
                            }
                        }

                        // If recompile is required, CompilerAnalyzer contains all the files which need to recompile.

                        // Cleanup baml files and code files generated in previous build.
                        if (TaskFileService.IsRealBuild)
                        {
                            CleanupGeneratedFiles();
                        }

                        // Call the Markup Compiler to do the real compiling work
                        DoMarkupCompilation();
                    }


                    // Generate the required output items.
                    GenerateOutputItems();

                    Log.LogMessageFromResources(MessageImportance.Low, SRID.CompilationDone);
                }

            }
#pragma warning disable 6500
            catch (Exception e)
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

                _nErrors++;
            }
            catch // Non-CLS compliant errors
            {
                Log.LogErrorWithCodeFromResources(SRID.NonClsError);
                _nErrors++;
            }
#pragma warning restore 6500

            if (_nErrors > 0)
            {
                // When error counter is changed, the appropriate error message should have
                // been reported, simply return false here.
                bSuccess = false;
                CleanupCacheFiles();
            }
            else
            {
                Log.LogMessageFromResources(MessageImportance.Low, SRID.CompileSucceed_Pass1);
            }

            return bSuccess;
        }


        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The Language the managed compiler supports.
        /// the valid languages are C#, VB, Jscript, J#, C++
        /// </summary>
        [Required]
        public string Language
        {
            get { return _language; }
            set { _language = value;}
        }

        /// <summary>
        /// The valid source file extension for the passed language.
        /// Normally a language supports more valid source file extensions.
        /// User could choose one of them in project file.
        /// If this property is not set, we will take use of the default one for the language.
        /// </summary>
        public string LanguageSourceExtension
        {
            get { return _languageSourceExtension; }
            set { _languageSourceExtension = value; }
        }

        ///<summary>
        /// OutputPath : Generated code files, Baml fles will be put in this directory.
        ///</summary>
        [Required]
        public string OutputPath
        {
            get { return _outputDir; }
            set
            {
                string filePath = value;

                // Get the relative path based on sourceDir
                _outputDir= TaskHelper.CreateFullFilePath(filePath, SourceDir);

                // Make sure OutputDir always ends with Path.DirectorySeparatorChar
                if (!_outputDir.EndsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    _outputDir += Path.DirectorySeparatorChar;
                }
            }
        }

        ///<summary>
        /// OutputType
        ///       Valid types: winexe, exe, library, netmodule.
        ///</summary>
        [Required]
        public string OutputType
        {
            get { return _outputType; }
            set { _outputType = TaskHelper.GetLowerString(value); }
        }

        ///<summary>
        /// AssemblyName
        /// The short name of assembly which will be generated for this project.
        ///</summary>
        [Required]
        public string AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }

        /// <summary>
        /// The version of the assembly
        /// </summary>
        public string AssemblyVersion
        {
            get { return _assemblyVersion;  }
            set { _assemblyVersion = value;  }
        }

        /// <summary>
        /// The public key token of the assembly
        /// </summary>
        public string AssemblyPublicKeyToken
        {
            get { return _assemblyPublicKeyToken; }
            set { _assemblyPublicKeyToken = value;  }
        }

        ///<summary>
        /// Root namespace for the classes inside the project.
        /// It is also used as default CLR namespace of a generated code file
        /// when the corresponding markup page is not set x:Class attribute.
        ///</summary>
        public string RootNamespace
        {
            get { return _rootNamespace; }
            set { _rootNamespace = value; }
        }

        /// <summary>
        /// The UI Culture controls which culture satellite assembly will hold
        /// the generated baml files.
        /// If UICulture is not set,  the generated baml files will be embedded
        /// into main assembly.
        /// </summary>
        public string UICulture
        {
            get { return _uiCulture; }
            set { _uiCulture = value; }
        }

        /// <summary>
        /// Source code file list for the current project.
        /// It doesnt include any generated code files.
        /// </summary>
        public ITaskItem[] SourceCodeFiles
        {
            get { return _sourceCodeFiles; }
            set { _sourceCodeFiles = value; }
        }

        /// <summary>
        /// DefineConstants
        ///
        /// Keep the current value of DefineConstants.
        ///
        /// DefineConstants can affect the final assembly generation, if DefineConstants
        /// value is changed, the public API might be changed in the target assembly, which
        /// then has potential impacts on compilation for xaml files which contains local types.
        ///
        /// </summary>
        public string DefineConstants
        {
            get { return _defineConstants; }
            set { _defineConstants = value; }
        }

        ///<summary>
        /// ApplicationMarkup
        ///</summary>
        public ITaskItem [] ApplicationMarkup
        {
            get { return _applicationMarkup; }
            set { _applicationMarkup = value;}
        }

        ///<summary>
        /// Description
        ///</summary>
        public ITaskItem [] PageMarkup
        {
            get { return _pagemarkupFiles;  }
            set { _pagemarkupFiles = value; }
        }

        ///<summary>
        /// Splash screen image to be displayed before application init
        ///</summary>
        public ITaskItem[] SplashScreen
        {
            get { return _splashScreen; }
            set { _splashScreen = value; }
        }

        internal string SplashImageName
        {
            get
            {
                if (SplashScreen != null && SplashScreen.Length > 0)
                {
                    return SplashScreen[0].ItemSpec.ToLowerInvariant();
                }
                return null;
            }
        }

        /// <summary>
        /// Loose file content list
        /// </summary>
        public ITaskItem[] ContentFiles
        {
            get { return _contentFiles; }
            set { _contentFiles = value; }
        }

        /// <summary>
        /// Assembly References.
        /// </summary>
        /// <value></value>
        public ITaskItem[] References
        {
            get { return _references; }
            set { _references = value; }
        }

        ///<summary>
        ///</summary>
        public bool XamlDebuggingInformation
        {
            get { return _xamlDebuggingInformation; }
            set { _xamlDebuggingInformation = value; }
        }

        /// <summary>
        /// Keep a list of Build control files.
        /// If one of them is changed since last build, it would trigger recompilation of all the xaml files.
        /// Such as WinFX target file change could require a rebuild etc.
        /// </summary>
        public ITaskItem [] ExtraBuildControlFiles
        {
            get { return _extraBuildControlFiles; }
            set { _extraBuildControlFiles = value; }
        }

        ///<summary>
        ///If true code for supporting hosting in Browser is generated
        ///</summary>
        public string HostInBrowser
        {
            get { return _hostInBrowser; }
            set { _hostInBrowser = TaskHelper.GetLowerString(value); }
        }

        /// <summary>
        /// Controls how to generate localization information for each xaml file.
        /// Valid values: None, CommentsOnly, All.
        /// </summary>
        public string LocalizationDirectivesToLocFile
        {
            get
            {
                string localizationDirectives = SharedStrings.Loc_None;

                switch (_localizationDirectives)
                {
                    case MS.Internal.LocalizationDirectivesToLocFile.None :

                        localizationDirectives = SharedStrings.Loc_None;
                        break;

                    case MS.Internal.LocalizationDirectivesToLocFile.CommentsOnly:

                        localizationDirectives = SharedStrings.Loc_CommentsOnly;
                        break;

                    case MS.Internal.LocalizationDirectivesToLocFile.All:

                        localizationDirectives = SharedStrings.Loc_All;

                        break;

                }

                return localizationDirectives;

            }

            set
            {
                string localizationDirectives = value;

                if (localizationDirectives != null)
                {
                    localizationDirectives = localizationDirectives.ToLower(CultureInfo.InvariantCulture);
                }

                switch (localizationDirectives)
                {
                    case SharedStrings.Loc_None :

                        _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.None;
                        break;

                    case SharedStrings.Loc_CommentsOnly :

                        _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.CommentsOnly;
                        break;

                    case SharedStrings.Loc_All :

                        _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.All;
                        break;

                    default:
                        _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.Unknown;
                        break;
                }

            }
        }

        /// <summary>
        /// Known reference paths hold referenced assemblies which are never changed during the build procedure.
        /// such as references in GAC, in framework directory or framework SDK directory etc.
        /// Users could add their own known reference paths in project files.
        /// </summary>
        public string[] KnownReferencePaths
        {
            get
            {
                return _knownReferencePaths;
            }

            set
            {
                _knownReferencePaths = value;
            }
        }

        /// <summary>
        /// A list of reference assemblies that are to change for sure during the build cycle.
        ///
        /// Such as in VS.NET, if one project wants to reference another project's output, the
        /// second project's output could be put in AssembliesGeneratedDuringBuild list.
        /// Note: Once this property is set, it must contain the complete list of generated
        /// assemblies in this build solution.
        /// </summary>
        public string[] AssembliesGeneratedDuringBuild
        {
            get
            {
                return _assembliesGeneratedDuringBuild;
            }

            set
            {
                _assembliesGeneratedDuringBuild = value;
            }

        }

        /// <summary>
        /// Control whether to run the compilation in second appdomain.
        /// By default, it is set to true, but project can set this property
        /// to false to make markup file compilation faster.
        /// </summary>
        public bool AlwaysCompileMarkupFilesInSeparateDomain
        {
            get { return _alwaysCompileMarkupFilesInSeparateDomain;   }
            set { _alwaysCompileMarkupFilesInSeparateDomain = value;  }
        }

        /// <summary>
        /// Set to true when called from Visual Studio.
        /// <summary>
        public bool IsRunningInVisualStudio
        {
            get { return _isRunningInVisualStudio;   }
            set { _isRunningInVisualStudio = value;  }
        }

        ///<summary>
        /// Generated source code files for the given programing language.
        ///</summary>
        [Output]
        public ITaskItem [] GeneratedCodeFiles
        {
            get
            {
               if (_generatedCodeFiles == null)
                   _generatedCodeFiles = Array.Empty<TaskItem>();
               return _generatedCodeFiles;
            }

            set
            {
                _generatedCodeFiles = value;
            }
        }

        ///<summary>
        /// Generated Baml files for the passed Markup files.
        ///</summary>
        [Output]
        public ITaskItem [] GeneratedBamlFiles
        {
            get
            {
               if (_generatedBamlFiles == null)
                   _generatedBamlFiles = Array.Empty<TaskItem>();
               return _generatedBamlFiles;
            }

            set
            {
                _generatedBamlFiles = value;
            }
        }

        /// <summary>
        /// The generated localization file for each localizable xaml file.
        /// </summary>
        [Output]
        public ITaskItem[] GeneratedLocalizationFiles
        {
            get
            {
                if (_generatedLocalizationFiles == null)
                    _generatedLocalizationFiles = Array.Empty<TaskItem>();

                return _generatedLocalizationFiles;
            }

            set
            {
                _generatedLocalizationFiles = value;
            }

        }

        #region Local Reference Xaml markup files

        /// <summary>
        /// Indicate whether the project contains xaml files which reference local types and
        /// the corresponding baml file will be embedded into main assembly.
        /// </summary>
        [Output]
        public bool RequirePass2ForMainAssembly
        {
            get { return _requirePass2ForMainAssembly; }
            set { _requirePass2ForMainAssembly = value; }
        }

        /// <summary>
        /// Indicate whether the project contains xaml files which reference local types and
        /// the corresponding baml file will be embedded into satellite assembly for current UICulture.
        /// </summary>
        [Output]
        public bool RequirePass2ForSatelliteAssembly
        {
            get { return _requirePass2ForSatelliteAssembly; }
            set { _requirePass2ForSatelliteAssembly = value; }
        }

        #endregion Local Reference Xaml markup files

        /// <summary>
        /// A complete list of files which are generated by MarkupCompiler.
        /// </summary>
        [Output]
        public ITaskItem[] AllGeneratedFiles
        {
            get { return _allGeneratedFiles; }
            set { _allGeneratedFiles = value; }
        }

        #endregion Public Properties

        #region internal methods

        //
        // Get the generated code file and baml file for a given xaml file
        // If the generated file doesn't exist, the output paramter is set to empty string.
        //
        internal void GetGeneratedFiles(string xamlFile, out string codeFile, out string bamlFile)
        {
            string newSourceDir = SourceDir;
            codeFile = String.Empty;
            bamlFile = String.Empty;

            if (String.IsNullOrEmpty(xamlFile))
            {
                // if xaml file is empty, return it now.
                return;
            }


            string relativeFilePath = GetResolvedFilePath(xamlFile, ref newSourceDir);

            // Optimize the number of intermediate strings that get generated
            // in the common C# case.  (we can add Vb here as well, I suppose)
            string buildExtension = (LanguageSourceExtension == SharedStrings.CsExtension)
                            ? SharedStrings.CsBuildCodeExtension
                            : SharedStrings.GeneratedExtension + LanguageSourceExtension;

            string intellisenseExtension = (LanguageSourceExtension == SharedStrings.CsExtension)
                            ? SharedStrings.CsIntelCodeExtension
                            : SharedStrings.IntellisenseGeneratedExtension + LanguageSourceExtension;

            string buildCodeFile = OutputPath + Path.ChangeExtension(relativeFilePath, buildExtension);
            string intelCodeFile = OutputPath + Path.ChangeExtension(relativeFilePath, intellisenseExtension);

            // If the file doesn't exist, return empty string for the corresponding output parameter.
            // return .g.i files for intellisense builds
            // return .g & .baml files for MsBuild builds (and real VS builds).
            if (TaskFileService.IsRealBuild)
            {
                codeFile = buildCodeFile;
                bamlFile = OutputPath + Path.ChangeExtension(relativeFilePath, SharedStrings.BamlExtension);

                if (!TaskFileService.Exists(codeFile))
                {
                    codeFile = String.Empty;
                }
                // Baml file is only generated for real build.
                if (!TaskFileService.Exists(bamlFile))
                {
                    bamlFile = String.Empty;
                }
            }
            else
            {
                codeFile = intelCodeFile;
                if (!TaskFileService.Exists(codeFile))
                {
                    codeFile = String.Empty;
                }
            }
        }

        #endregion internal methods


        #region internal Properties

        internal bool IsApplicationTarget
        {
            get { return _isApplicationTarget; }
        }

        //
        // ApplicationFile
        //
        internal string ApplicationFile
        {
            get { return _applicationFile; }
        }

        //
        // PageMarkupCache
        // It is caculated from current PageMarkup list, will be saved in the
        // cache file to support incremental compilation.
        //
        internal string PageMarkupCache
        {
            get { return _pageMarkupCache; }
        }

        //
        // ContentFilesCache
        // It is caculated from current ContentFiles list, will be saved in the
        // cache file to support incremental compilation.
        //
        internal string ContentFilesCache
        {
            get { return _contentFilesCache; }
        }

        //
        // SourceCodeFilesCache
        // It is caculated from current SourceCodeFiles list, will be saved in the
        // cache file to support incremental compilation.
        //
        internal string SourceCodeFilesCache
        {
            get { return _sourceCodeFilesCache; }
        }

        //
        // ReferencesCache
        // It is caculated from current ReferencesCache list, will be saved in the
        // cache file to support incremental compilation.
        //
        internal string ReferencesCache
        {
            get { return _referencesCache; }
        }

        //
        // Application File with Local Type
        //
        internal LocalReferenceFile LocalApplicationFile
        {
            get { return _localApplicationFile; }
        }

        //
        // Markup Page files with local types
        //
        internal LocalReferenceFile[] LocalMarkupPages
        {
            get { return _localMarkupPages; }
        }

        //
        // TaskFileService
        //
        internal ITaskFileService TaskFileService
        {
            get { return _taskFileService; }
        }

        //
        // CompilerState
        //
        internal CompilerState CompilerState
        {
            get { return _compilerState; }
        }

        //
        // CompilerLocalReference
        //
        internal CompilerLocalReference CompilerLocalReference
        {
            get { return _compilerLocalRefCache; }
        }

        //
        // Tell MarkupCompilePass2 whether it further handles InternalTypeHelper class.
        //
        internal bool FurtherCheckInternalTypeHelper
        {
            get { return _furtherCheckInternalTypeHelper; }
            set { _furtherCheckInternalTypeHelper = value; }
        }

        //
        // Get the file path for the generated InternalTypeHelper class.
        //
        internal string InternalTypeHelperFile
        {
            get
            {
                string fileName = SharedStrings.GeneratedInternalTypeHelperFileName
                    + (TaskFileService.IsRealBuild? SharedStrings.GeneratedExtension : SharedStrings.IntellisenseGeneratedExtension)
                    + LanguageSourceExtension;

                return Path.Combine(OutputPath, fileName);
            }
        }


        #endregion  internal Properties

        //------------------------------------------------------
        //
        // Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        //
        // Check if all the input properties are set valid values
        // If any property contains an invalid value, Error counter _nErrors will
        // be changed.
        //
        private void VerifyInputs()
        {
            // Check if the OutputType is valid type.
            IsSupportedOutputType(OutputType);

            // Check if the Localization property is set correctly.
            IsValidLocalizationDirectives();

            VerifyApplicationFile();

            if (PageMarkup != null && PageMarkup.Length > 0)
            {
                VerifyInputTaskItems(PageMarkup);
            }

            if (SplashScreen != null && SplashScreen.Length > 1)
            {
                Log.LogErrorWithCodeFromResources(SRID.MultipleSplashScreenImages);
                _nErrors++;
            }
        }

        //
        // Verify if the Application file is set correctly
        // in project file.
        //
        private void VerifyApplicationFile()
        {
            if (!IsApplicationTarget)
            {
                //
                // For non Application target type.
                //
                if (ApplicationMarkup != null && ApplicationMarkup.Length > 0)
                {
                    //
                    // For non-Application target type, Application definition should not be set.
                    //
                    Log.LogErrorWithCodeFromResources(SRID.AppDefIsNotRequired);
                    _nErrors++;

                }

            }
            else
            {
                //
                // For Application Target type.
                //
                if (ApplicationMarkup != null && ApplicationMarkup.Length > 0)
                {
                    if (ApplicationMarkup.Length > 1)
                    {
                        Log.LogErrorWithCodeFromResources(SRID.MutlipleApplicationFiles);
                        _nErrors++;
                    }

                    _applicationFile = TaskHelper.CreateFullFilePath(ApplicationMarkup[0].ItemSpec, SourceDir);
                    Log.LogMessageFromResources(MessageImportance.Low, SRID.ApplicationDefinitionFile, ApplicationFile);

                    if (!TaskFileService.Exists(ApplicationFile))
                    {
                        Log.LogErrorWithCodeFromResources(SRID.FileNotFound, ApplicationFile);
                        _nErrors++;
                    }

                }

            }
        }

        //
        // Don't support local reference xaml compilation for Container and netmodule type.
        //
        private bool IsSupportedOutputType(string outputType)
        {
            bool isSupported = false;

            switch (outputType)
            {
                case SharedStrings.Exe:
                case SharedStrings.WinExe:
                    isSupported = true;
                    _isApplicationTarget = true;
                    break;
                case SharedStrings.Library:
                case SharedStrings.Module:
                    isSupported = true;
                    break;

                default:
                    isSupported = false;
                    break;
            }

            if (isSupported == false)
            {
                Log.LogErrorWithCodeFromResources(SRID.TargetIsNotSupported, outputType);

                // Keep the error numbers so that the task can stop immediatelly
                // later when Execute( ) is called.
                _nErrors++;
            }

            return isSupported;
        }

        private bool IsValidLocalizationDirectives()
        {
            bool bValid = true;

            if (_localizationDirectives == MS.Internal.LocalizationDirectivesToLocFile.Unknown)
            {
                bValid = false;

                Log.LogErrorWithCodeFromResources(SRID.WrongLocalizationPropertySetting_Pass1);

                // Keep the error numbers so that the task can stop immediatelly
                // later when Execute( ) is called.
                _nErrors++;
            }

            return bValid;
        }


        // <summary>
        // Check if the passed TaskItems have valid ItemSpec
        // </summary>
        // <param name="inputItems"></param>
        // <returns></returns>
        private bool VerifyInputTaskItems(ITaskItem[] inputItems)
        {
            bool bValid = true;

            foreach (ITaskItem inputItem in inputItems)
            {
                bool bValidItem;

                bValidItem = IsValidInputFile(inputItem.ItemSpec);

                if (bValidItem == false)
                {
                    bValid = false;
                }
            }

            return bValid;
        }

        private bool IsValidInputFile(string filePath)
        {
            bool bValid = true;

            if (!TaskFileService.Exists(TaskHelper.CreateFullFilePath(filePath, SourceDir)))
            {
                bValid = false;
                Log.LogErrorWithCodeFromResources(SRID.FileNotFound, filePath);

                // Keep the error numbers so that the task can stop immediatelly
                // later when Execute( ) is called.
                _nErrors ++;

            }

            return bValid;
        }

        //
        // Return a new sourceDir and relative filepath for a given filePath.
        // This is for supporting of fullpath or ..\ in the original FilePath.
        //
        private string GetResolvedFilePath(string filePath, ref string newSourceDir)
        {
            // Create a full path for the originalFilePath.
            string fullFilePath = TaskHelper.CreateFullFilePath(filePath, SourceDir);

            // Get the relative path based on sourceDir
            string relPath = TaskHelper.GetRootRelativePath(SourceDir, fullFilePath);
            string newRelativeFilePath;

            if (relPath.Length > 0)
            {
                // the original file is relative to the SourceDir.
                newSourceDir = SourceDir;
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
                newRelativeFilePath = TaskHelper.GetRootRelativePath(newSourceDir, fullFilePath);
            }

            return newRelativeFilePath;
        }

        //
        // Analyze the project setting and input files for incremental build support.
        private void AnalyzeInputsAndSetting()
        {
            // Initialize the cache file paths and related information.

            _pageMarkupCache = CompilerState.GenerateCacheForFileList(PageMarkup);
            _contentFilesCache = CompilerState.GenerateCacheForFileList(ContentFiles);
            _sourceCodeFilesCache = CompilerState.GenerateCacheForFileList(SourceCodeFiles);
            _referencesCache = CompilerState.GenerateCacheForFileList(References);

            _compilerAnalyzer = new IncrementalCompileAnalyzer(this);

            _compilerAnalyzer.AnalyzeInputFiles();


            _isCleanBuild = (CompilerAnalyzer.AnalyzeResult == RecompileCategory.All) ? true : false;

        }

        //
        // Specially handle Reference list to prepare for xaml file compilation.
        //
        private ArrayList ProcessReferenceList( )
        {
            ArrayList referenceList = new ArrayList();

            // Generate the asmmebly reference list.
            if (References != null && References.Length > 0)
            {
                ReferenceAssembly asmReference;
                string refpath, asmname;

                for (int i = 0; i < References.Length; i++)
                {
                    // The reference path must be full file path.
                    refpath = References[i].ItemSpec;
                    refpath = TaskHelper.CreateFullFilePath(refpath, SourceDir);

                    asmname = Path.GetFileNameWithoutExtension(refpath);

                    asmReference = new ReferenceAssembly(refpath, asmname);
                    referenceList.Add(asmReference);

                    Log.LogMessageFromResources(MessageImportance.Low, SRID.ReferenceFile, refpath);
                }
            }

            return referenceList;
        }

        // Cleanup baml files and code files generated in previous build.
        private void CleanupGeneratedFiles( )
        {
            string codeFile, bamlFile;

            if (IsApplicationTarget && !String.IsNullOrEmpty(CompilerAnalyzer.RecompileApplicationFile.Path))
            {
                GetGeneratedFiles(CompilerAnalyzer.RecompileApplicationFile.Path, out codeFile, out bamlFile);

                if (!String.IsNullOrEmpty(codeFile))
                {
                    TaskFileService.Delete(codeFile);
                }

                if (!String.IsNullOrEmpty(bamlFile))
                {
                    TaskFileService.Delete(bamlFile);
                }
            }

            if (CompilerAnalyzer.RecompileMarkupPages != null)
            {
                for (int i = 0; i < CompilerAnalyzer.RecompileMarkupPages.Length; i++)
                {
                    GetGeneratedFiles(CompilerAnalyzer.RecompileMarkupPages[i].Path, out codeFile, out bamlFile);

                    if (!String.IsNullOrEmpty(codeFile))
                    {
                        TaskFileService.Delete(codeFile);
                    }

                    if (!String.IsNullOrEmpty(bamlFile))
                    {
                        TaskFileService.Delete(bamlFile);
                    }
                }
            }

            // If the content file setting is changed, the generated content.g.cs code file should be updated later,
            // so delete the file here first.
            //
            // This includes the scenario that all the previous content files are removed from the content item list in
            // this build run.
            if ((CompilerAnalyzer.AnalyzeResult & RecompileCategory.ContentFiles) == RecompileCategory.ContentFiles)
            {
                if (TaskFileService.Exists(ContentCodeFile))
                {
                    TaskFileService.Delete(ContentCodeFile);
                }
            }

            // If this is for CleanBuild, and the InternalTypeHelper file exists, delete it first.
            if (IsCleanBuild && TaskFileService.Exists(InternalTypeHelperFile))
            {
                TaskFileService.Delete(InternalTypeHelperFile);
            }
        }

        //
        // Call MarkupCompiler to do the real compilation work.
        //
        private void DoMarkupCompilation()
        {
            Log.LogMessageFromResources(MessageImportance.Low, SRID.DoCompilation);
            Log.LogMessageFromResources(MessageImportance.Low, SRID.OutputType, OutputType);


            // When code goes here, the MarkupCompilation is really required, so don't need
            // to do more further validation inside this private method.

            AppDomain appDomain = null;
            CompilerWrapper compilerWrapper = null;

            try
            {
                compilerWrapper = TaskHelper.CreateCompilerWrapper(AlwaysCompileMarkupFilesInSeparateDomain, ref appDomain);

                if (compilerWrapper != null)
                {
                    compilerWrapper.OutputPath = OutputPath;
                    compilerWrapper.AssemblyVersion = AssemblyVersion;
                    compilerWrapper.AssemblyPublicKeyToken = AssemblyPublicKeyToken;
                    compilerWrapper.LanguageSourceExtension = LanguageSourceExtension;
                    compilerWrapper.HostInBrowser = TaskHelper.BooleanStringValue(HostInBrowser);
                    compilerWrapper.SplashImage = SplashImageName;

                    compilerWrapper.TaskLogger = Log;
                    compilerWrapper.UnknownErrorID = UnknownErrorID;
                    compilerWrapper.XamlDebuggingInformation = XamlDebuggingInformation;

                    compilerWrapper.TaskFileService = TaskFileService;

                    if (IsApplicationTarget)
                    {
                        compilerWrapper.ApplicationMarkup = CompilerAnalyzer.RecompileApplicationFile;
                    }

                    compilerWrapper.ContentFiles = CompilerAnalyzer.ContentFiles;

                    // Process Reference list here.
                    ArrayList referenceList = ProcessReferenceList();

                    compilerWrapper.References = referenceList;

                    compilerWrapper.LocalizationDirectivesToLocFile = (int)_localizationDirectives;

                    compilerWrapper.DoCompilation(AssemblyName, Language, RootNamespace, CompilerAnalyzer.RecompileMarkupPages, false);

                    // Keep the Local-Type-Ref file lists

                    _localXamlPages = compilerWrapper.LocalXamlPages;
                    _localXamlApplication = compilerWrapper.LocalXamlApplication;

                    _hasInternals = compilerWrapper.HasInternals;
                }
            }
            finally
            {

                if (compilerWrapper != null && compilerWrapper.ErrorTimes > 0)
                {
                    _nErrors += compilerWrapper.ErrorTimes;
                }

                if (appDomain != null)
                {
                    System.Threading.Tasks.Task.Run(() => 
                    {
                        // Better GC behavior in 4.6 and later when wrapped in Task.Run().
                        // Inside of VisualStudio, when DesignTimeMarkupCompilation happens, it uses MarkupCompilePass1 only (not Pass2).
                        AppDomain.Unload(appDomain);
                    });
                }

                compilerWrapper = null;
            }
        }


        // <summary>
        // Generate the required Output Items.
        // </summary>
        private void GenerateOutputItems( )
        {
            // For the rest target types,
            // Create the output lists for CS and Baml files.

            ArrayList bamlFileList = new ArrayList();
            ArrayList csFileList = new ArrayList();
            ArrayList localRefPageList = new ArrayList();
            ArrayList localRefAppdefList = new ArrayList();

            // Generate Output Items for PageMarkup
            if (PageMarkup != null && PageMarkup.Length > 0)
            {
                GenerateOutputItemsForCompiledXamlFiles(PageMarkup,
                                                        _localXamlPages,
                                                        ref bamlFileList,
                                                        ref csFileList,
                                                        ref localRefPageList);
            }

            //
            // Generate output items for ApplicationDefinition if it is set in the project file.
            //
            if (ApplicationFile != null && ApplicationFile.Length > 0)
            {
                string[] appdefLocalList = null;

                if (!String.IsNullOrEmpty(_localXamlApplication))
                {
                    appdefLocalList = new string[1] { _localXamlApplication };
                }

                GenerateOutputItemsForCompiledXamlFiles(ApplicationMarkup,
                                          appdefLocalList,
                                          ref bamlFileList,
                                          ref csFileList,
                                          ref localRefAppdefList);
            }

            if (TaskFileService.Exists(ContentCodeFile))
            {
                csFileList.Add(new TaskItem(ContentCodeFile));
            }

            // Generate the Baml, code and /or locally-defined type related output items.
            GeneratedBamlFiles = (ITaskItem[])bamlFileList.ToArray(typeof(ITaskItem));

            if (!SkipMarkupCompilation)
            {
                if (localRefAppdefList.Count > 0)
                {
                    _localApplicationFile = (LocalReferenceFile)localRefAppdefList[0];
                }

                if (localRefPageList.Count > 0)
                {
                    _localMarkupPages = (LocalReferenceFile[])localRefPageList.ToArray(typeof(LocalReferenceFile));
                }

                //
                // If MarkupCompilePass2 is required for Main assembly, there is no need to invoke MarkupCompilePass2
                // for satellite assembly again.
                //
                if (_requirePass2ForMainAssembly)
                {
                    _requirePass2ForSatelliteAssembly = false;
                }
            }

            //
            // Detect whether or not to ask Pass2 to do further handling for the InternalTypeHelper class.
            //
            // Only when all of below conditions are true, it requires Pass2 to further handling this wrapper class:
            //
            //    1.    InternalTypeHelper file exists.
            //    2.    It is a CleanBuild.
            //    3.    No any xaml files which don't contain local types contains internal types from friend assembly.
            //    4.    _requirePass2ForMainAssembly is true.
            //
            // If InternalTypeHelper File exists, Pass1 would always add it to the code file list, so that consequent task
            // can take it.  If Pass2 determines that this wrapper class is not required, it can simply make an empty file.
            // But we still keep the list of generated code files.

            bool existsInternalTypeHelper = TaskFileService.Exists(InternalTypeHelperFile);

            if (IsCleanBuild && existsInternalTypeHelper && _requirePass2ForMainAssembly && !_hasInternals)
            {
                FurtherCheckInternalTypeHelper = true;
            }
            else
            {
                FurtherCheckInternalTypeHelper = false;
            }

            if (existsInternalTypeHelper)
            {
                csFileList.Add(new TaskItem(InternalTypeHelperFile));
            }

            GeneratedCodeFiles = (ITaskItem[])csFileList.ToArray(typeof(ITaskItem));

            // Generate the Localization Output files
            if (_localizationDirectives != MS.Internal.LocalizationDirectivesToLocFile.None)
            {
                GenerateOutputItemsForLocFiles();
            }

            HandleCacheFiles();

            //
            // Put all the generated files into one output Item so that it can be set to
            // FileWrites item in target file, this list of files will be cleaned up for
            // next clean build by msbuild.
            // The generated files should include Baml files, code files, localization files
            // and the cache files.
            //

            ArrayList allGeneratedFiles = new ArrayList( );

            for (int i = 0; i < GeneratedBamlFiles.Length; i++)
            {
                allGeneratedFiles.Add(GeneratedBamlFiles[i]);
            }

            for (int i = 0; i < GeneratedCodeFiles.Length; i++)
            {
                allGeneratedFiles.Add(GeneratedCodeFiles[i]);
            }

            for (int i = 0; i < GeneratedLocalizationFiles.Length; i++)
            {
                allGeneratedFiles.Add(GeneratedLocalizationFiles[i]);
            }

            // Add the CompilerState cache file into the list

            allGeneratedFiles.Add(new TaskItem(CompilerState.CacheFilePath));

            if (CompilerLocalReference.CacheFileExists())
            {
                allGeneratedFiles.Add(new TaskItem(CompilerLocalReference.CacheFilePath));
            }

            AllGeneratedFiles = (ITaskItem[])allGeneratedFiles.ToArray(typeof(ITaskItem));

        }


        //
        // Both MarkupPage and MarkupResource have the similar code to generate
        // output baml, code file and /or locallyDefined xaml files.
        // so put all the common code in this private method.
        //
        // Inputs :
        //            Xaml file items:  PageMarkup or MarkupResource
        //            LocallyDefined Xaml List (Generated by MarkupCompiler)
        //
        // Outputs:   BamlFile List,
        //            CodeFile List,
        //            LocallyDefined Xaml List
        //
        private void GenerateOutputItemsForCompiledXamlFiles(ITaskItem[] inputXamlItemList,
                                                             string[]   inputLocalRefXamlFileList,
                                                             ref ArrayList outputBamlFileList,
                                                             ref ArrayList outputCodeFileList,
                                                             ref ArrayList outputLocalRefXamlList)
        {

            //
            // For each input xaml file, check if the code and baml file are generated or not.
            // If baml or code files are generated, put them into the appropriate output file item list.
            //
            for (int i = 0; i < inputXamlItemList.Length; i++)
            {
                string genLangFilePath, bamlFile;

                GetGeneratedFiles(inputXamlItemList[i].ItemSpec, out genLangFilePath, out bamlFile);

                if (!String.IsNullOrEmpty(genLangFilePath) && outputCodeFileList != null)
                {
                    TaskItem codeItem;

                    codeItem = new TaskItem();
                    codeItem.ItemSpec = genLangFilePath;
                    
                    outputCodeFileList.Add(codeItem);

                    Log.LogMessageFromResources(MessageImportance.Low, SRID.GeneratedCodeFile, codeItem.ItemSpec);
                }

                if (!String.IsNullOrEmpty(bamlFile))
                {
                    TaskItem bamlItem = GenerateBamlItem(bamlFile, inputXamlItemList[i]);

                    // Add bamlItem to the output Baml List
                    outputBamlFileList.Add(bamlItem);
                    Log.LogMessageFromResources(MessageImportance.Low, SRID.GeneratedBamlFile, bamlItem.ItemSpec);
                }

            }  // End of for {  } loop.


            //
            // If the project contains local-type xaml files, put them into the right output item list here.
            //

            //
            // If MarkupCompilation is skipped, there is no need to check local-type xaml files.
            //
            if (!SkipMarkupCompilation && inputLocalRefXamlFileList != null && inputLocalRefXamlFileList.Length > 0)
            {
                for (int i = 0; i < inputLocalRefXamlFileList.Length; i++)
                {
                    string fullLocalXamlFile = TaskHelper.CreateFullFilePath(inputLocalRefXamlFileList[i], SourceDir);

                    LocalReferenceFile localFile = GenerateLocalTypeItem(fullLocalXamlFile, inputXamlItemList);

                    if (localFile != null && outputLocalRefXamlList != null)
                    {
                        outputLocalRefXamlList.Add(localFile);
                    }
                }
            }
        }


        //
        // Generate appropriate output file list for local-type xaml file. This information will be saved
        // into the .lref cache file so that the MarkupCompilePass2 can take it.
        //
        private LocalReferenceFile GenerateLocalTypeItem(string localTypeXamlFile, ITaskItem[] inputXamlItemList)
        {
            LocalReferenceFile localFile = null;
            bool isLocalizable = false;
            string linkAlias = String.Empty;
            string logicalName = String.Empty;

            //
            // Check if the local-type xaml file is localizable or not.
            //
            for (int i = 0; i < inputXamlItemList.Length; i++)
            {
                ITaskItem inputXamlItem = inputXamlItemList[i];

                string xamlInputFullPath = TaskHelper.CreateFullFilePath(inputXamlItem.ItemSpec, SourceDir);

                if (String.Compare(localTypeXamlFile, xamlInputFullPath, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    //
                    // Got this file from the original XamlFile TaskItem list.
                    // Check if this item is localizable or not and stop the search here.
                    //
                    isLocalizable = IsItemLocalizable(inputXamlItem);
                    linkAlias = inputXamlItem.GetMetadata(SharedStrings.Link);
                    logicalName = inputXamlItem.GetMetadata(SharedStrings.LogicalName);
                    break;
                }
            }

            //
            // Generate the instance of LocalReferenceFile for this local-type xaml file.
            //
            localFile = new LocalReferenceFile(localTypeXamlFile, isLocalizable, linkAlias, logicalName);

            if (isLocalizable)
            {
                _requirePass2ForSatelliteAssembly = true;
            }
            else
            {
                _requirePass2ForMainAssembly = true;
            }

            return localFile;
        }



        //
        // Generate a baml TaskItem for the given xaml file, and transfer the appropriate
        // source task item's custom attributes to the generated baml item if necessary.
        // The xaml file could be an application definition file, a Markup Page.
        // The bamlFile must exist before this method is called.
        private TaskItem GenerateBamlItem(string bamlFile, ITaskItem SourceItem)
        {
            TaskItem bamlItem;

            bamlItem =  new TaskItem();
            bamlItem.ItemSpec = bamlFile;

            //
            // Transfer some special custom attributes from source task item
            // to output item.
            // Such as transfer the Localizable attribute from a given .xaml file item
            // to the generated .baml file item.
            //
            // Normally MarkupPage and MarkupResource need to transfer their attributes.
            // But Application definition doesn't require this.
            if (SourceItem != null)
            {
                string[] listCarryOverAttribute = new string[] {
                    SharedStrings.Localizable,
                    SharedStrings.Link,
                    SharedStrings.LogicalName
                };

                for (int j = 0; j < listCarryOverAttribute.Length; j++)
                {
                    string attributeValue;

                    attributeValue = SourceItem.GetMetadata(listCarryOverAttribute[j]);
                    if (attributeValue != null)
                    {
                        bamlItem.SetMetadata(listCarryOverAttribute[j], attributeValue);
                    }
                }
            }

            return bamlItem;
        }

        //
        // Generate output item list for localization files.
        //
        private void GenerateOutputItemsForLocFiles()
        {

            ArrayList locFileItemList = new ArrayList();
            TaskItem  tiLoc;
            ITaskItem xamlItem;

            if (ApplicationMarkup != null && ApplicationMarkup.Length > 0 && ApplicationMarkup[0] != null)
            {

                tiLoc = ProcessLocFileForXamlItem(ApplicationMarkup[0]);

                if (tiLoc != null)
                {
                    // Add this LocItem to the locFileItemList
                    locFileItemList.Add(tiLoc);
                }
            }

            if (PageMarkup != null)
            {
                for (int i = 0; i < PageMarkup.Length; i++)
                {
                    xamlItem = PageMarkup[i];

                    tiLoc = ProcessLocFileForXamlItem(xamlItem);

                    if (tiLoc != null)
                    {
                        // Add this LocItem to the locFileItemList
                        locFileItemList.Add(tiLoc);
                    }
                }
            }

            // Generate the Output TaskItem List
            GeneratedLocalizationFiles = (ITaskItem[])locFileItemList.ToArray(typeof(ITaskItem));

        }

        //
        // General method to handle an input xaml file item for Localization.
        //
        // If the XamlFile is localizable, generate a TaskItem for LocFile.
        // If the XamlFile is not localizable, and if the .loc file is generated, delete it
        // so that it won't affect the incremental build next time.
        //
        private TaskItem ProcessLocFileForXamlItem(ITaskItem xamlItem)
        {
            TaskItem tiLoc = null;

            string   tempDir = SourceDir;  // Just for calling GetResolvedFilePath, the value is not used here.

            // Get a relative file path for the passed .xaml file
            string xamlRelativeFilePath = GetResolvedFilePath(xamlItem.ItemSpec, ref tempDir);
            string locFile;

            // Change the extension from .xaml to .loc
            locFile = Path.ChangeExtension(xamlRelativeFilePath, SharedStrings.LocExtension);

            // the .loc file is at OutputPath + relative Path.
            locFile = OutputPath + locFile;

            if (TaskFileService.Exists(locFile))
            {
               //
               // Per discussion with Globalization team:
               //
               //   Globalization requests to collect .loc file for a baml file
               //   no matter the baml is in main assembly or satelliate assembly.
               //
               //   The localization tool can localize the baml from the main assembly as well.
               //

                // Generate a TaskItem to include .loc file
                // The item is going to add to the output LocFile list
                tiLoc = new TaskItem(locFile);
            }

            return tiLoc;
        }

        //
        // Check if the given TaskItem localizable.
        //
        private bool IsItemLocalizable(ITaskItem ti)
        {
            bool bIsLocalizable;

            if (String.IsNullOrEmpty(UICulture))
            {
                // if UICulture is not set, all baml files are not localizable.
                // The Localizable metadate value is ignored for this case.
                bIsLocalizable = false;
            }
            else
            {
                string strLocalizable;

                // if UICulture is set, by default all the baml files are localizable unless
                // an explicit value "false" is set to Localizable metadata.
                bIsLocalizable = true;

                strLocalizable = ti.GetMetadata(SharedStrings.Localizable);

                if (strLocalizable != null && String.Compare(strLocalizable, "false", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    bIsLocalizable = false;
                }
            }

            return bIsLocalizable;
        }

        //
        // Cleanup the cache files.
        // It could happen if build error occurs.
        //
        private void CleanupCacheFiles()
        {
            CompilerState.CleanupCache();
            CompilerLocalReference.CleanupCache();
        }


        //
        // A central place to update cache files based on current build status MarkupCompiler result.
        //
        // This method should be called after the markupcompiler is done or skipped.
        //
        private void HandleCacheFiles()
        {
            // Update the CompilerState file based on new project setting, no matter MarkupCompiler is skipped or not.
            CompilerState.SaveStateInformation(this);

            if ( (CompilerAnalyzer.AnalyzeResult & ( RecompileCategory.PagesWithLocalType |  RecompileCategory.ModifiedPages )) != RecompileCategory.NoRecompile)
            {
                // The modified xaml files and all the local-type xaml files should be recompiled, depends on the
                // MarkupCompiler return, it will keep or delete the cache file for local type xaml files.

                if (_requirePass2ForMainAssembly || _requirePass2ForSatelliteAssembly)
                {
                    CompilerLocalReference.SaveCacheInformation(this);
                }
                else
                {
                    CompilerLocalReference.CleanupCache();
                }
            }

            // if build doesn't handle the local-ref xaml files, (it implies not handling the modified xaml files either),
            // such as the build handles only for Application (HIB change) and Content files changes, or the markup compilation
            // is skipped, ( NoRecompile).
            //
            // For this scenario, if .lref file exists, it should still be kept.
            //

        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        // <summary>
        // The root directory for the applicaiton project.
        // </summary>
        private string SourceDir
        {
            get { return _sourceDir; }
        }

        private IncrementalCompileAnalyzer CompilerAnalyzer
        {
            get { return _compilerAnalyzer; }
        }


        //
        // If no input file is modified, Markup compilation will be skipped.
        // But the code still generates the correct output item lists.
        //
        private bool SkipMarkupCompilation
        {
            get { return CompilerAnalyzer.AnalyzeResult == RecompileCategory.NoRecompile; }
        }

        private string ContentCodeFile
        {
            get
            {
                return OutputPath + AssemblyName + SharedStrings.ContentFile
                    + (TaskFileService.IsRealBuild? SharedStrings.GeneratedExtension : SharedStrings.IntellisenseGeneratedExtension)
                    + LanguageSourceExtension;
            }
        }

        //
        // Whether this is a clean build or not
        //
        private bool IsCleanBuild
        {
            get { return _isCleanBuild; }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private string                     _language;
        private string                     _languageSourceExtension = string.Empty;
        private ITaskItem []               _pagemarkupFiles;
        private ITaskItem []               _contentFiles;
        private ITaskItem []               _references;
        private bool                       _xamlDebuggingInformation = false;
        private string                     _outputType;
        private string                     _assemblyName;
        private string                     _assemblyVersion;
        private string                     _assemblyPublicKeyToken;
        private string                     _rootNamespace = String.Empty;
        private ITaskItem []               _applicationMarkup;
        private ITaskItem[]                _splashScreen;
        private bool                       _alwaysCompileMarkupFilesInSeparateDomain = true;
        private bool                       _isRunningInVisualStudio;

        private string[]                   _assembliesGeneratedDuringBuild;
        private string[]                   _knownReferencePaths;

        private string                     _sourceDir;
        private string                     _outputDir;

        private ITaskItem[]                _extraBuildControlFiles;
        private string                     _uiCulture = String.Empty;

        private string                     _applicationFile = String.Empty;
        private bool                       _isApplicationTarget = false;
        private string                     _hostInBrowser = String.Empty;

        private LocalizationDirectivesToLocFile _localizationDirectives;

        private ITaskItem []               _generatedCodeFiles;
        private ITaskItem []               _generatedBamlFiles;
        private ITaskItem []               _generatedLocalizationFiles;
        private ITaskItem []               _allGeneratedFiles = null;

        private string                     _localXamlApplication;
        private string[]                   _localXamlPages;
        private bool                       _hasInternals = false;

        private int                        _nErrors;

        private string                     _defineConstants = String.Empty;
        private ITaskItem[]                _sourceCodeFiles;

        private string                     _pageMarkupCache = String.Empty;
        private string                     _contentFilesCache = String.Empty;
        private string                     _sourceCodeFilesCache = String.Empty;
        private string                     _referencesCache = String.Empty;
        private LocalReferenceFile         _localApplicationFile = null;
        private LocalReferenceFile[]       _localMarkupPages = null;

        private bool                       _requirePass2ForMainAssembly = false;
        private bool                       _requirePass2ForSatelliteAssembly = false;

        private bool                       _furtherCheckInternalTypeHelper = false;

        private CompilerState              _compilerState;
        private CompilerLocalReference     _compilerLocalRefCache;
        private IncrementalCompileAnalyzer _compilerAnalyzer;

        private ITaskFileService           _taskFileService;

        private bool                       _isCleanBuild = true;   // Indicates whether this is a cleanbuild or incremental build.

        #region const string

        private const string UnknownErrorID = "MC1000";

        #endregion const string

        #endregion Private Fields

    }

    #endregion MarkupCompilePass1 Task class
}
