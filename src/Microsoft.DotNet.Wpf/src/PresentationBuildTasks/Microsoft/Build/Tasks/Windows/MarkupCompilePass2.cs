// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description: A MSBuild Task that can generate .baml file for some special
//              xaml markup files that want to take some locally-defined types.
//
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Security;
using System.Text;

using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

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

    #region MarkupCompilePass2 Task class

    /// <summary>
    /// Class of MarkupCompilePass2 Task
    /// </summary>
    public sealed class MarkupCompilePass2  : Task
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
        public MarkupCompilePass2( ) : base(SR.SharedResourceManager)
        {
            // set the source directory
            _sourceDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;

            _outputType = SharedStrings.WinExe;

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
            TaskHelper.DisplayLogo(Log, nameof(MarkupCompilePass2));

            //
            // Create the TaskFileService instance here
            //
            _taskFileService = new TaskFileService(this) as ITaskFileService;

            try
            {
                IsSupportedOutputType(OutputType);

                Log.LogMessageFromResources(MessageImportance.Low, SRID.CurrentDirectory, SourceDir);

                // If wrong files are set to some properties, the task
                // should stop here immediatelly.

                if (_nErrors > 0)
                {
                    Log.LogErrorWithCodeFromResources(SRID.WrongPropertySetting);
                }
                else
                {
                    bool hasLocalXamlFiles;

                    hasLocalXamlFiles = InitLocalXamlCache();

                    if (!hasLocalXamlFiles)
                    {
                        // There is no valid input xaml files.
                        // No need to do further work.
                        // stop here.
                        return true;
                    }

                    // create output directory
                    if (!Directory.Exists(OutputPath))
                    {
                        Directory.CreateDirectory(OutputPath);
                    }

                    // Call the Markup Compiler to do the real compiling work

                    ArrayList referenceList;
                    FileUnit localApplicationFile;
                    FileUnit[] localXamlPageFileList;

                    // Prepare the appropriate file lists required by MarkupCompiler.
                    PrepareForMarkupCompilation(out localApplicationFile, out localXamlPageFileList, out referenceList);

                    // Do the real Pass2 compilation work here.
                    DoLocalReferenceMarkupCompilation(localApplicationFile, localXamlPageFileList, referenceList);

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
                // been reported.

                //
                // The task should cleanup all the cache files so that all the xaml files will
                // get chance to recompile next time.
                //

                string stateFileName = OutputPath + AssemblyName +
                                      (TaskFileService.IsRealBuild? SharedStrings.StateFile : SharedStrings.IntellisenseStateFile);

                string localTypeCacheFileName = OutputPath + AssemblyName +
                                      (TaskFileService.IsRealBuild? SharedStrings.LocalTypeCacheFile : SharedStrings.IntellisenseLocalTypeCacheFile);

                if (TaskFileService.Exists(stateFileName))
                {
                    TaskFileService.Delete(stateFileName);
                }

                if (TaskFileService.Exists(localTypeCacheFileName))
                {
                    TaskFileService.Delete(localTypeCacheFileName);
                }

                return false;
            }
            else
            {
                // Mark Pass2 as completed in the cache
                string stateFileName = OutputPath + AssemblyName +
                      (TaskFileService.IsRealBuild ? SharedStrings.StateFile : SharedStrings.IntellisenseStateFile);
                if (TaskFileService.Exists(stateFileName))
                {
                    CompilerState compilerState = new CompilerState(stateFileName, TaskFileService);
                    compilerState.LoadStateInformation();
                    if (compilerState.Pass2Required)
                    {
                        compilerState.Pass2Required = false;
                        compilerState.SaveStateInformation();
                    }
                }

                Log.LogMessageFromResources(SRID.CompileSucceed_Pass2);
                return true;
            }
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
            set { _language = value; }
        }

        ///<summary>
        /// OutputPath
        /// Directory which will contain the generated baml files.
        ///</summary>
        [Required]
        public string OutputPath
        {
            get { return _outputPath; }
            set
            {
                string filePath = value;

                // Get the relative path based on sourceDir
                _outputPath= TaskHelper.CreateFullFilePath(filePath, SourceDir);

                // Make sure OutputDir always ends with Path.DirectorySeparatorChar
                if (!_outputPath.EndsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    _outputPath += Path.DirectorySeparatorChar;
                }
            }
        }

        ///<summary>
        /// OutputType
        /// Valid types: exe, winexe, library, netmodule
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

        ///<summary>
        /// Root namespace for the classes inside the project.
        /// It is also used in the Root element record of the generated baml file
        /// when the corresponding markup page is not set x:Class attribute.
        ///</summary>
        public string RootNamespace
        {
            get { return _rootNamespace; }
            set { _rootNamespace = value; }
        }

        /// <summary>
        /// Control whether to run the compilation in second appdomain.
        /// By default, it is set to true, but project can set this property
        /// to false to make markup file compilation faster.
        /// </summary>
        public bool AlwaysCompileMarkupFilesInSeparateDomain
        {
            get { return _alwaysCompileMarkupFilesInSeparateDomain; }
            set { _alwaysCompileMarkupFilesInSeparateDomain = value; }
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

        ///<summary>
        /// Generated Baml files for the passed markup xaml files
        ///</summary>
        [Output]
        public ITaskItem [] GeneratedBaml
        {
            get
            {
               if (_generatedBaml == null)
                   _generatedBaml = Array.Empty<TaskItem>();
               return _generatedBaml;
            }

            set
            {
                _generatedBaml = value;
            }
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
                    case MS.Internal.LocalizationDirectivesToLocFile.None:

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
                    case SharedStrings.Loc_None:

                        _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.None;
                        break;

                    case SharedStrings.Loc_CommentsOnly:

                        _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.CommentsOnly;
                        break;

                    case SharedStrings.Loc_All:

                        _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.All;
                        break;

                    default:
                        _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.Unknown;
                        break;
                }

            }
        }


        #endregion Public Properties

        //------------------------------------------------------
        //
        // Private Properties
        //
        //------------------------------------------------------

        //
        // TaskFileService
        //
        private ITaskFileService TaskFileService
        {
            get { return _taskFileService; }
        }


        //------------------------------------------------------
        //
        // Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        //
        // Initialze the local xaml cache file.
        //
        // return value:
        //
        //    If cache doesn't exist, or both LocalAppDef and LocallXaml Pages do not exist, return false
        //    to indicate no further work required.
        //    otherwise, return true.
        //
        private bool InitLocalXamlCache()
        {
            bool hasLocalFiles = false;

            _compilerLocalRefCache = new CompilerLocalReference(
                         OutputPath + AssemblyName + (TaskFileService.IsRealBuild? SharedStrings.LocalTypeCacheFile : SharedStrings.IntellisenseLocalTypeCacheFile),
                        _taskFileService);

            if (_compilerLocalRefCache.CacheFileExists())
            {
                _compilerLocalRefCache.LoadCacheFile();

                _localApplicationFile = _compilerLocalRefCache.LocalApplicationFile;
                _localMarkupPages = _compilerLocalRefCache.LocalMarkupPages;

                if (_localApplicationFile != null || (_localMarkupPages != null && _localMarkupPages.Length > 0))
                {
                    hasLocalFiles = true;

                    //
                    // Initialize InternalTypeHelper file from the cache file first.
                    // Further handling will be taken after the xaml file compilation is done.
                    //
                    // If InternalTypeHelperFile is set in the Cache file, it means Pass1 cannot
                    // detect whether or not to keep the InternalTypeHelper File until the Pass2
                    // xaml file compilation is done.
                    //
                    _internalTypeHelperFile = _compilerLocalRefCache.InternalTypeHelperFile;
                }
            }

            return hasLocalFiles;
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
        // Generate the necessary file lists and other information required by MarkupCompiler.
        //
        // Output ArrayLists:  localApplicationFile,
        //                     localXamlPageFileList
        //                     referenceList
        //
        private void PrepareForMarkupCompilation(out FileUnit localApplicationFile, out FileUnit[] localXamlPageFileList, out ArrayList referenceList)
        {
            Log.LogMessageFromResources(MessageImportance.Low, SRID.PreparingCompile);
            Log.LogMessageFromResources(MessageImportance.Low, SRID.OutputType, OutputType);

            // Initialize the output parameters
            localXamlPageFileList = Array.Empty<FileUnit>();
            localApplicationFile = FileUnit.Empty;
            referenceList = new ArrayList();

            if (_localApplicationFile != null)
            {
                // We don't want to support multiple application definition file per project.
                localApplicationFile = new FileUnit(_localApplicationFile.FilePath, _localApplicationFile.LinkAlias, _localApplicationFile.LogicalName);

                Log.LogMessageFromResources(MessageImportance.Low, SRID.LocalRefAppDefFile, localApplicationFile);

            }

            // Generate the Xaml Markup file list
            if (_localMarkupPages != null && _localMarkupPages.Length > 0)
            {
                int localFileNum = _localMarkupPages.Length;
                localXamlPageFileList = new FileUnit[localFileNum];

                for (int i = 0; i < localFileNum; i++)
                {
                    FileUnit localPageFile = new FileUnit(_localMarkupPages[i].FilePath, _localMarkupPages[i].LinkAlias, _localMarkupPages[i].LogicalName);

                    localXamlPageFileList[i] = localPageFile;
                    Log.LogMessageFromResources(MessageImportance.Low, SRID.LocalRefMarkupPage, localPageFile);
                }
            }

            //
            // Generate the asmmebly reference list.
            // The temporay target assembly should have been added into Reference list from target file.
            //
            if (References != null && References.Length > 0)
            {
                ReferenceAssembly asmReference;
                string refpath, asmname;

                for (int i = 0; i < References.Length; i++)
                {
                    refpath = References[i].ItemSpec;
                    refpath = TaskHelper.CreateFullFilePath(refpath, SourceDir);

                    asmname = Path.GetFileNameWithoutExtension(refpath);

                    asmReference = new ReferenceAssembly(refpath, asmname);
                    referenceList.Add(asmReference);
                }
            }

        }

        //
        // Call MarkupCompiler to do the real compilation work.
        //
        private void DoLocalReferenceMarkupCompilation(FileUnit localApplicationFile, FileUnit[] localXamlPageFileList, ArrayList referenceList)
        {
            // When code goes here, the MarkupCompilation is really required, so don't need
            // to do more further validation inside this private method.

            Log.LogMessageFromResources(MessageImportance.Low, SRID.DoCompilation);

            AppDomain appDomain = null;
            CompilerWrapper compilerWrapper = null;

            try
            {
                compilerWrapper = TaskHelper.CreateCompilerWrapper(AlwaysCompileMarkupFilesInSeparateDomain, ref appDomain);

                if (compilerWrapper != null)
                {

                    compilerWrapper.OutputPath = OutputPath;

                    compilerWrapper.TaskLogger = Log;
                    compilerWrapper.UnknownErrorID = UnknownErrorID;
                    compilerWrapper.XamlDebuggingInformation = XamlDebuggingInformation;

                    compilerWrapper.TaskFileService = _taskFileService;

                    if (OutputType.Equals(SharedStrings.Exe) || OutputType.Equals(SharedStrings.WinExe))
                    {
                        compilerWrapper.ApplicationMarkup = localApplicationFile;
                    }

                    compilerWrapper.References = referenceList;

                    compilerWrapper.LocalizationDirectivesToLocFile = (int)_localizationDirectives;

                    // This is for Pass2 compilation
                    compilerWrapper.DoCompilation(AssemblyName, Language, RootNamespace, localXamlPageFileList, true);

                    //
                    // If no any xaml file with local-types wants to reference an internal type from
                    // current assembly and friend assembly, and InternalTypeHelperFile is set in the
                    // cache file, now it is the time to remove the content of InternalTypeHelper File.
                    //
                    // We still keep the empty file to make other parts of the build system happy.
                    //
                    if (!String.IsNullOrEmpty(_internalTypeHelperFile) && !compilerWrapper.HasInternals)
                    {
                        if (TaskFileService.Exists(_internalTypeHelperFile))
                        {
                            // Make empty content for this file.

                            MemoryStream memStream = new MemoryStream();

                            using (StreamWriter writer = new StreamWriter(memStream, new UTF8Encoding(false)))
                            {
                                writer.WriteLine(String.Empty);
                                writer.Flush();
                                TaskFileService.WriteFile(memStream.ToArray(), _internalTypeHelperFile);
                            }

                            Log.LogMessageFromResources(MessageImportance.Low, SRID.InternalTypeHelperNotRequired, _internalTypeHelperFile);
                        }
                    }
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
                    AppDomain.Unload(appDomain);
                    compilerWrapper = null;
                }
            }

        }

        // <summary>
        // Generate the required Output Items.
        // </summary>
        private void GenerateOutputItems( )
        {
            // For the rest target types,
            // Create the output lists for Baml files.
            ArrayList bamlFileList = new ArrayList();
            string    newSourceDir = SourceDir;  // Just for calling GetResolvedFilePath
            string    relativeFile;

            if (_localApplicationFile != null)
            {
                TaskItem bamlItem;

                relativeFile = GetResolvedFilePath(_localApplicationFile.FilePath, ref newSourceDir);

                bamlItem = GenerateBamlItem(relativeFile, _localApplicationFile.Localizable, _localApplicationFile.LinkAlias, _localApplicationFile.LogicalName);

                if (bamlItem != null)
                {
                    bamlFileList.Add(bamlItem);

                    Log.LogMessageFromResources(MessageImportance.Low, SRID.LocalRefGeneratedBamlFile, bamlItem.ItemSpec);
                }
            }

            if (_localMarkupPages != null && _localMarkupPages.Length > 0)
            {

                for (int i = 0; i < _localMarkupPages.Length; i++)
                {
                    // add the baml file
                    LocalReferenceFile localRefFile = _localMarkupPages[i];

                    relativeFile = GetResolvedFilePath(localRefFile.FilePath, ref newSourceDir);

                    TaskItem bamlItem = GenerateBamlItem(relativeFile, localRefFile.Localizable, localRefFile.LinkAlias, localRefFile.LogicalName);

                    if (bamlItem != null)
                    {
                        bamlFileList.Add(bamlItem);
                        Log.LogMessageFromResources(MessageImportance.Low, SRID.LocalRefGeneratedBamlFile, bamlItem.ItemSpec);
                    }
                }
            }

            // Generate the  Baml Output Item
            GeneratedBaml = (ITaskItem[])bamlFileList.ToArray(typeof(ITaskItem));

        }

        //
        // Generate a baml TaskItem for the given xaml file, and transfer the appropriate
        // source task item's custom attributes to the generated baml item if necessary.
        // The xaml file could be an application definition file or a Markup Page
        //
        // Note: the xaml file must be resolved by calling GetResolvedFilePath( ) or
        // CreatFullFilePath( ) before calling this method.
        //
        private TaskItem GenerateBamlItem(string resolvedXamlfile, bool localizable, string linkAlias, string logicalName)
        {
            TaskItem bamlItem = null;

            //
            // For a given .xaml file (foo.xaml), there are below options for generated file:
            //
            //    1.  A baml file with the same xaml file base name. foo.baml (such as page)
            //    2.  No baml file generated. such as  logical component,
            //                                      or some simple Application definition xaml.
            //

            string bamlFileName = Path.ChangeExtension(resolvedXamlfile, SharedStrings.BamlExtension);

            string bamlFile = OutputPath + bamlFileName;


            if (TaskFileService.Exists(bamlFile))
            {
                //
                // Baml file exists.
                // Generate a TaskItem for it.
                //

                bamlItem = new TaskItem();
                bamlItem.ItemSpec = bamlFile;

                // Transfer the metadata value from source item to the generated baml item.
                bamlItem.SetMetadata(SharedStrings.Localizable, localizable ?  "True" : "False");
                bamlItem.SetMetadata(SharedStrings.Link, linkAlias);
                bamlItem.SetMetadata(SharedStrings.LogicalName, logicalName);
            }

            return bamlItem;
        }


        //
        // Don't support local reference xaml compilation for netmodule type.
        //
        private bool IsSupportedOutputType(string outputType)
        {
            bool isSupported = false;

            switch (outputType)
            {
                case SharedStrings.Exe :
                case SharedStrings.WinExe:
                case SharedStrings.Library :
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

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private ITaskItem []               _references;
        private string                     _outputType;
        private string                     _assemblyName;
        private string[]                   _assembliesGeneratedDuringBuild;
        private string[]                   _knownReferencePaths;
        private string                     _rootNamespace = String.Empty;
        private bool                       _xamlDebuggingInformation = false;

        private bool                       _alwaysCompileMarkupFilesInSeparateDomain = true;

        private LocalizationDirectivesToLocFile _localizationDirectives = MS.Internal.LocalizationDirectivesToLocFile.None;

        private string                     _sourceDir;
        private string                     _outputPath;
        private string                     _language;

        private ITaskItem []               _generatedBaml;

        private int                        _nErrors;

        private CompilerLocalReference     _compilerLocalRefCache;
        private LocalReferenceFile         _localApplicationFile = null;
        private LocalReferenceFile[]       _localMarkupPages = null;
        private string                     _internalTypeHelperFile = String.Empty;

        private ITaskFileService           _taskFileService;


        private const string UnknownErrorID = "MC2000";

        #endregion Private Fields

    }

    #endregion MarkupCompilePass2 Task class
}
