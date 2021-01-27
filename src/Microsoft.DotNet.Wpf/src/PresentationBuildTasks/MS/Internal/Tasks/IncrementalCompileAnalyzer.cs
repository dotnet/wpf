// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//----------------------------------------------------------------------------------------
// 
// Description:
//       Analyze the current project inputs, the compiler state file and local reference 
//       cache, determine which xaml files require to recompile.
//
//---------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.Build.Tasks.Windows;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MS.Utility;

namespace MS.Internal.Tasks
{
    //
    // Keep different categories of recompilation.
    //
    [Flags]
    internal enum RecompileCategory : byte
    {
        NoRecompile        = 0x00,
        ApplicationFile    = 0x01,
        ModifiedPages      = 0x02,
        PagesWithLocalType = 0x04,
        ContentFiles       = 0x08,
        All                = 0x0F
    }

    // <summary>
    // IncrementalCompileAnalyzer
    // </summary>
    internal class IncrementalCompileAnalyzer  
    {
        // <summary>
        // ctor of IncrementalCompileAnalyzer
        // </summary>
        internal IncrementalCompileAnalyzer(MarkupCompilePass1 mcPass1)
        {
            _mcPass1 = mcPass1;
            _analyzeResult = RecompileCategory.NoRecompile;
        }

        #region internal methods

        public static void LogCompilerState(string filePrefix, CompilerState compilerState)
        {
          using (StreamWriter w = File.AppendText($"{filePrefix}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.txt"))
          {
              w.WriteLine($"AssemblyName: {compilerState.AssemblyName}");                  
              w.WriteLine($"AssemblyVersion: {compilerState.AssemblyVersion}");
              w.WriteLine($"AssemblyPublicKeyToken: {compilerState.AssemblyPublicKeyToken}");
              w.WriteLine($"OutputType: {compilerState.OutputType}");
              w.WriteLine($"Language: {compilerState.Language}");
              w.WriteLine($"LanguageSourceExtension: {compilerState.LanguageSourceExtension}");
              w.WriteLine($"OutputPath: {compilerState.OutputPath}");
              w.WriteLine($"RootNamespace: {compilerState.RootNamespace}");
              w.WriteLine($"LocalizationDirectivesToLocFile: {compilerState.LocalizationDirectivesToLocFile}");
              w.WriteLine($"HostInBrowser: {compilerState.HostInBrowser}");
              w.WriteLine($"DefineConstants: {compilerState.DefineConstants}");
              w.WriteLine($"ApplicationFile: {compilerState.ApplicationFile}");
              w.WriteLine($"PageMarkupCache: ");
              w.WriteLine($"ContentFilesCache: ");
              w.WriteLine($"SourceCodeFilesCache: ");
              w.WriteLine($"References: {compilerState.References}");
              w.WriteLine($"PageMarkup: ");
              w.WriteLine($"SplashImageName: ");
              w.WriteLine($"RequirePass2ForMainAssembly: "); 
              w.WriteLine($"RequirePass2ForSatelliteAssembly: ");
              w.WriteLine($"Pass2Required: ");
          }

        }

        public static void Log(string logString)
        {
          using (StreamWriter w = File.AppendText($"RecompileCategory_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm")}.txt"))
          {
            w.WriteLine(logString);
          }
        }

        // 
        // Analyze the input files based on the compiler cache files.
        // 
        // Put the analyze result in _analyzeResult and other related data fields,
        // such as RecompileMarkupPages, RecompileApplicationFile, etc.
        //
        // If analyze is failed somehow, throw exception.
        //
        internal void AnalyzeInputFiles()
        {
            MS.Internal.Tasks.CompilerState.LogMarkupCompilePass1State("MarkupCompilePass1State",_mcPass1);

            //
            // First: Detect if the entire project requires re-compile.
            //

            //
            // If the compiler state file doesn't exist, recompile all the xaml files.
            //
            if (!CompilerState.StateFileExists())
            {
                Log("StateFile does not exist.  Recompiling all...");
                _analyzeResult = RecompileCategory.All;
            }
            else
            {
                // Load the compiler state file.
                CompilerState.LoadStateInformation();
                LogCompilerState("PreviousCompilerState", CompilerState);

                // if PresenationBuildTasks.dll is changed last build, rebuild the entire project for sure.

                if (IsFileChanged(Assembly.GetExecutingAssembly().Location) ||
                    IsFileListChanged(_mcPass1.ExtraBuildControlFiles))
                {
                    _analyzeResult = RecompileCategory.All;
                }
                else
                {
                    //
                    // Any one single change in below list would request completely re-compile.
                    //
                    if (IsSettingModified(CompilerState.References, _mcPass1.ReferencesCache))
                    { 
                        Log("References list has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.ApplicationFile, _mcPass1.ApplicationFile))
                    { 
                        Log("ApplicationFile has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.RootNamespace, _mcPass1.RootNamespace))
                    { 
                        Log("RootNamespace has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.AssemblyName, _mcPass1.AssemblyName))
                    { 
                        Log("AssemblyName has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.AssemblyVersion, _mcPass1.AssemblyVersion))
                    { 
                        Log("AssemblyVersion has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.AssemblyPublicKeyToken, _mcPass1.AssemblyPublicKeyToken))
                    { 
                        Log("AssemblyPublicKeyToken has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.OutputType, _mcPass1.OutputType))
                    { 
                        Log("OutputType has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.Language, _mcPass1.Language))
                    { 
                        Log("Language has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.LanguageSourceExtension, _mcPass1.LanguageSourceExtension))
                    { 
                        Log("LanguageSourceExtension has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.OutputPath, _mcPass1.OutputPath))
                    { 
                        Log("OutputPath has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All; 
                    }
                    else if(IsSettingModified(CompilerState.LocalizationDirectivesToLocFile, _mcPass1.LocalizationDirectivesToLocFile))
                    {
                        Log("LocalizationDirectivesToLocFile has changed.  Recompiling all...");
                        _analyzeResult = RecompileCategory.All;
                    }
                    else
                    {
                        if (_mcPass1.IsApplicationTarget)
                        {
                            //
                            // When application definition file is modified, it could potentially change the application
                            // class name, it then has impact on all other xaml file compilation, so recompile the entire
                            // project for this case.
                            //
                            if (TaskFileService.Exists(_mcPass1.ApplicationFile) && IsFileChanged(_mcPass1.ApplicationFile))
                            {
                                Log("ApplicationFile has changed.  Recompiling all...");
                                _analyzeResult = RecompileCategory.All;
                            }
                        }

                        // 
                        // If any one referenced assembly is updated since last build, the entire project needs to recompile.
                        //

                        if (IsFileListChanged(_mcPass1.References))
                        {
                            Log("One of the Reference files has changed.  Recompiling all...");
                            _analyzeResult = RecompileCategory.All;
                        }
                    }
                }
            }

            if (_analyzeResult == RecompileCategory.All)
            {
                UpdateFileListForCleanbuild();
                return;
            }

            //
            // The entire project recompilation should have already been handled when code goes here.
            // Now, Detect the individual xaml files which require to recompile.
            //

            if (_mcPass1.IsApplicationTarget)
            {
                if (IsSettingModified(CompilerState.ContentFiles, _mcPass1.ContentFilesCache))
                {
                    Log("One of the Content files has changed.  Recompiling ContentFiles...");
                    _analyzeResult |= RecompileCategory.ContentFiles;
                }

                // if HostInBrowser setting is changed, it would affect the application file compilation only.
                if (IsSettingModified(CompilerState.HostInBrowser, _mcPass1.HostInBrowser))
                {
                    Log("HostInBrowser setting has changed.  Recompiling ApplicationFile...");
                    _analyzeResult |= RecompileCategory.ApplicationFile;
                }

                if (IsSettingModified(CompilerState.SplashImage, _mcPass1.SplashImageName))
                {
                    Log("SplashImage setting has changed.  Recompiling ApplicationFile...");
                    _analyzeResult |= RecompileCategory.ApplicationFile;
                }
            }

            //
            // If code files are changed, or Define flags are changed, it would affect the xaml file with local types.
            //
            // If previous build didn't have such local-ref-xaml files, don't bother to do further check for this.
            //

            if (CompilerLocalReference.CacheFileExists())
            {
                if (IsSettingModified(CompilerState.DefineConstants, _mcPass1.DefineConstants) ||
                    IsSettingModified(CompilerState.SourceCodeFiles, _mcPass1.SourceCodeFilesCache) || 
                    IsFileListChanged(_mcPass1.SourceCodeFiles) )
                {
                    Log("DefineConstants OR SourceCodeFiles have been modified or file has changed.  Recompiling PagesWithLocalType...");
                    _analyzeResult |= RecompileCategory.PagesWithLocalType;
                }
            }

            List<FileUnit> modifiedXamlFiles = new List<FileUnit>();

            //
            // Detect if any .xaml page is updated since last build
            //
            if (ListIsNotEmpty(_mcPass1.PageMarkup))
            {

                //
                // If the PageMarkup file number or hashcode is changed, it would affect
                // the xaml files with local types.
                //
                // This check is necessary for the senario that a xaml file is removed and the
                // removed xaml file could be referenced by other xaml files with local types.
                //
                if (IsSettingModified(CompilerState.PageMarkup, _mcPass1.PageMarkupCache))
                {
                    if (CompilerLocalReference.CacheFileExists())
                    {
                        Log("PageMarkup file has changed.  Recompiling PagesWithLocalType...");
                        _analyzeResult |= RecompileCategory.PagesWithLocalType;
                    }
                }

                // Below code detects which individual xaml files are modified since last build.
                for (int i = 0; i < _mcPass1.PageMarkup.Length; i++)
                {
                    ITaskItem taskItem = _mcPass1.PageMarkup[i];
                    string fileName = taskItem.ItemSpec;
                    string filepath = Path.GetFullPath(fileName);
                    string linkAlias = taskItem.GetMetadata(SharedStrings.Link);
                    string logicalName = taskItem.GetMetadata(SharedStrings.LogicalName);

                    if (IsFileChanged(filepath))
                    {
                        // add this file to the modified file list.
                        Log($"{filepath} detected as modified.  Adding to modififed file list...");
                        modifiedXamlFiles.Add(new FileUnit(filepath, linkAlias, logicalName));
                    }
                    else
                    {
                        // A previously generated xaml file (with timestamp earlier than of the cache file) 
                        // could be added to the project.  This means that the above check for time stamp 
                        // will skip compiling the newly added xaml file.  We save the name all the xaml 
                        // files we previously built to the cache file.  Retrieve that list and see if 
                        // this xaml file is already in it.  If so, we'll skip compiling this xaml file, 
                        // else, this xaml file was just added to the project and thus compile it.
                        
                        if (!CompilerState.PageMarkupFileNames.Contains(fileName))
                        {
                            Log($"{filepath} detected as new.  Adding to modififed file list...");
                            modifiedXamlFiles.Add(new FileUnit(filepath, linkAlias, logicalName));
                        }
                    }
                }

                if (modifiedXamlFiles.Count > 0)
                {
                    Log($"Modified XAML files counts is greater than 0. Recomping ModifiedPages...");
                    _analyzeResult |= RecompileCategory.ModifiedPages;

                    if (CompilerLocalReference.CacheFileExists())
                    {
                        Log($"CacheFile exists. Recompiling PagesWithLocalType...");
                        _analyzeResult |= RecompileCategory.PagesWithLocalType;
                    }
                }      
            }

            // Check for the case where a required Pass2 wasn't run, e.g. because the build was aborted,
            // or because the Compile target was run inside VS.
            // If that happened, let's recompile the local-type pages, which will force Pass2 to run.
            if (CompilerState.Pass2Required && CompilerLocalReference.CacheFileExists())
            {
                Log($"!!! Compiler Pass2Required and CacheFileExists. Recompiling PagesWithLocalType...");
                _analyzeResult |= RecompileCategory.PagesWithLocalType;
            }

            UpdateFileListForIncrementalBuild(modifiedXamlFiles);

        }

        #endregion

        #region internal properties

        //
        // Keep the AnlyzeResult.
        //
        internal RecompileCategory AnalyzeResult
        {
            get { return _analyzeResult; }
        }

        //
        // Keep a list of markup pages which require to recompile
        //
        internal FileUnit[] RecompileMarkupPages
        {
            get { return _recompileMarkupPages; }
        }

        //
        // Application file which requires re-compile.
        // If the value is String.Empty, the appdef file is not required
        // to recompile.
        //
        internal FileUnit RecompileApplicationFile
        {
            get { return _recompileApplicationFile; }
        }


        internal string[] ContentFiles
        {
            get { return _contentFiles; }
        }

        #endregion

        #region private properties and methods

        private CompilerState CompilerState
        {
            get { return _mcPass1.CompilerState; }
        }

        private CompilerLocalReference CompilerLocalReference
        {
            get { return _mcPass1.CompilerLocalReference; }
        }

        private ITaskFileService TaskFileService
        {
            get { return _mcPass1.TaskFileService; }
        }
        
        private DateTime LastCompileTime
        {
            get
            {
                DateTime nonSet = new DateTime(0);
                if (_lastCompileTime == nonSet)
                {
                    _lastCompileTime = TaskFileService.GetLastChangeTime(CompilerState.CacheFilePath);

                }

                return _lastCompileTime;
            }
        }

        //
        // Compare two strings.
        //
        private bool IsSettingModified(string textSource, string textTarget)
        {
            bool IsSettingModified;

            bool isSrcEmpty = String.IsNullOrEmpty(textSource);
            bool istgtEmpty = String.IsNullOrEmpty(textTarget);

            if (isSrcEmpty != istgtEmpty)
            {
                IsSettingModified = true;
            }
            else
            {
                if (isSrcEmpty)  // Both source and target strings are empty.
                {
                    IsSettingModified = false;
                }
                else  // Both source and target strings are not empty.
                {
                    IsSettingModified = String.Compare(textSource, textTarget, StringComparison.OrdinalIgnoreCase) == 0 ? false : true;
                }
            }

            return IsSettingModified;
        }

        //
        // Generate new list of files that require to recompile for incremental build based on _analyzeResult
        //
        private void UpdateFileListForIncrementalBuild(List<FileUnit> modifiedXamlFiles)
        {
            List<FileUnit> recompiledXaml = new List<FileUnit>();
            bool recompileApp = false;
            int numLocalTypeXamls = 0;

            if ((_analyzeResult & RecompileCategory.ContentFiles) == RecompileCategory.ContentFiles)
            {
                RecompileContentFiles();
            }

            if ((_analyzeResult & RecompileCategory.ApplicationFile) == RecompileCategory.ApplicationFile)
            {
                recompileApp = true;
            }

            if ((_analyzeResult & RecompileCategory.PagesWithLocalType) == RecompileCategory.PagesWithLocalType && TaskFileService.IsRealBuild)
            {
                CompilerLocalReference.LoadCacheFile();

                if (CompilerLocalReference.LocalApplicationFile != null)
                {
                    // Application file contains local types, it will be recompiled.
                    recompileApp = true;
                }

                if (ListIsNotEmpty(CompilerLocalReference.LocalMarkupPages))
                {
                    numLocalTypeXamls = CompilerLocalReference.LocalMarkupPages.Length;

                    // Under incremental builds of SDK projects, we can have a state where the cache contains some XAML files but the Page blob
                    // no longer contains them.  To avoid attempting to recompile a file that no longer exists, ensure that any cached XAML file
                    // still exists in the Page blob prior to queuing it up for recompilation.
                    HashSet<string> localMarkupPages = new HashSet<string>(_mcPass1.PageMarkup.Select(x => x.GetMetadata(SharedStrings.FullPath)), StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < numLocalTypeXamls; i++)
                    {
                        LocalReferenceFile localRefFile = CompilerLocalReference.LocalMarkupPages[i];

                        if (localMarkupPages.Contains(localRefFile.FilePath))
                        {
                            recompiledXaml.Add(new FileUnit(
                                                    localRefFile.FilePath,
                                                    localRefFile.LinkAlias,
                                                    localRefFile.LogicalName));
                        }
                    }
                }

            }

            if ((_analyzeResult & RecompileCategory.ModifiedPages) == RecompileCategory.ModifiedPages)
            {
                // If the xaml is already in the local-type-ref xaml file list, don't add a duplicate file path to recompiledXaml list.

                for (int i = 0; i < modifiedXamlFiles.Count; i++)
                {
                    FileUnit xamlfile = modifiedXamlFiles[i];
                    bool addToList;

                    addToList = true;

                    if (numLocalTypeXamls > 0)
                    {
                        for (int j = 0; j < numLocalTypeXamls; j++)
                        {
                            if (String.Compare(xamlfile.Path, CompilerLocalReference.LocalMarkupPages[j].FilePath, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                addToList = false;
                                break;
                            }
                        }
                    }

                    if (addToList)
                    {
                        recompiledXaml.Add(xamlfile);
                    }
                }
            }

            if (recompiledXaml.Count > 0)
            {
                _recompileMarkupPages = recompiledXaml.ToArray();
            }

            // Set ApplicationFile appropriatelly for this incremental build.
            ProcessApplicationFile(recompileApp);
        }

        //
        // To recompile all the xaml files ( including page and application file).
        // Transfer all the xaml files to the recompileMarkupPages.
        // 
        private void UpdateFileListForCleanbuild()
        {
            if (ListIsNotEmpty(_mcPass1.PageMarkup))
            {
                int count = _mcPass1.PageMarkup.Length;
                _recompileMarkupPages = new FileUnit[count];

                for (int i = 0; i < count; i++)
                {
                    ITaskItem taskItem = _mcPass1.PageMarkup[i];
                    _recompileMarkupPages[i] = new FileUnit( 
                        Path.GetFullPath(taskItem.ItemSpec), 
                        taskItem.GetMetadata(SharedStrings.Link),
                        taskItem.GetMetadata(SharedStrings.LogicalName));
                }
            }

            RecompileContentFiles();

            ProcessApplicationFile(true);
        }

        //
        // Content files are only for Application target type.
        //
        private void RecompileContentFiles()
        {
            if (!_mcPass1.IsApplicationTarget)
                return;

            if (_contentFiles == null)
            {
                if (ListIsNotEmpty(_mcPass1.ContentFiles))
                {
                    string curDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;

                    int count = _mcPass1.ContentFiles.Length;

                    _contentFiles = new string[count];

                    for (int i = 0; i < count; i++)
                    {
                        string fullPath = Path.GetFullPath(_mcPass1.ContentFiles[i].ItemSpec);

                        string relContentFilePath = TaskHelper.GetRootRelativePath(curDir, fullPath);

                        if (String.IsNullOrEmpty(relContentFilePath))
                        {
                           relContentFilePath = Path.GetFileName(fullPath);
                        }

                        _contentFiles[i] = relContentFilePath;
                    }

                }
            }
        }

        //
        // Handle Application definition xaml file and Application Class name.
        // recompile parameter indicates whether or not to recompile the appdef file.
        // If the appdef file is not recompiled, a specially handling is required to 
        // take application class name from previous build.
        //
        private void ProcessApplicationFile(bool recompile)
        {
            if (!_mcPass1.IsApplicationTarget)
            {
                return;
            }

            if (recompile)
            {
                //
                // Take whatever setting in _mcPass1 task.
                //
                if (_mcPass1.ApplicationMarkup != null && _mcPass1.ApplicationMarkup.Length > 0 && _mcPass1.ApplicationMarkup[0] != null)
                {
                    ITaskItem taskItem = _mcPass1.ApplicationMarkup[0];
                    _recompileApplicationFile = new FileUnit(
                                                    _mcPass1.ApplicationFile, 
                                                    taskItem.GetMetadata(SharedStrings.Link),
                                                    taskItem.GetMetadata(SharedStrings.LogicalName));
                }
                else
                {
                    _recompileApplicationFile = FileUnit.Empty;
                }
            }
            else
            {
                _recompileApplicationFile = FileUnit.Empty;

            }
        }

        //
        // Detect if at least one file in the same item list has changed since last build.
        //
        private bool IsFileListChanged(ITaskItem[] fileList)
        {
            bool isChanged = false;

            if (ListIsNotEmpty(fileList))
            {
                for (int i = 0; i < fileList.Length; i++)
                {
                    if (IsFileChanged(fileList[i].ItemSpec))
                    {
                        isChanged = true;
                        break;
                    }
                }
            }

            return isChanged;

        }

        public static void LogFileChanged(string filePrefix, string fileName, DateTime previousChange, DateTime thisChange)
        {
          StackFrame CallStack = new StackFrame(1, true);
          using (StreamWriter w = File.AppendText($"{filePrefix}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.txt"))
          {
             w.WriteLine($"{fileName} changed.  PreviousCompile: {previousChange} LastChanged: {thisChange}\n");
          }
        }

        //
        // Detect if the input file was changed since last build.
        //
        private bool IsFileChanged(string inputFile)
        {
            bool isChanged = false;

            DateTime dtFile;


            dtFile = TaskFileService.GetLastChangeTime(inputFile);

            if (dtFile > LastCompileTime)
            {
                isChanged = true;
                LogFileChanged("FileChangeDetected", inputFile, LastCompileTime, dtFile);
            }

            return isChanged;
        }

        // A helper to detect if the list is not empty.
        private bool ListIsNotEmpty(object [] list)
        {
            bool isNotEmpty = false;

            if (list != null && list.Length > 0)
            {
                isNotEmpty = true;
            }

            return isNotEmpty;
        }

        #endregion

        #region private data

        private MarkupCompilePass1 _mcPass1;
        private RecompileCategory  _analyzeResult;
        private DateTime           _lastCompileTime = new DateTime(0);

        private FileUnit[]         _recompileMarkupPages = null;
        private FileUnit           _recompileApplicationFile = FileUnit.Empty;
        private string[]           _contentFiles = null;

        #endregion

    }
}
