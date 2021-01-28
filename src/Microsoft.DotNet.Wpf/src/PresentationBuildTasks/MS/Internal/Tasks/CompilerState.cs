// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//----------------------------------------------------------------------------------------
//
// Description:
//       An internal class which handles compiler information cache. It can read
//       write the cache file, this is for incremental build support.
//
//---------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Globalization;

using Microsoft.Build.Tasks.Windows;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MS.Utility;

namespace MS.Internal.Tasks
{
    //
    // Only cache information for below types.
    //
    internal enum CompilerStateType : int
    {
        AssemblyName = 0x00,                  
        AssemblyVersion,
        AssemblyPublicKeyToken,
        OutputType,
        Language,
        LanguageSourceExtension,
        OutputPath,
        RootNamespace,
        LocalizationDirectivesToLocFile,
        HostInBrowser,
        DefineConstants,
        ApplicationFile,
        PageMarkup,
        ContentFiles,
        SourceCodeFiles,
        References,
        PageMarkupFileNames,
        SplashImage,
        Pass2Required,
        MaxCount,
    }

    // <summary>
    // CompilerState
    // </summary>
    internal class CompilerState  
    {
        // <summary>
        // ctor of CompilerState
        // </summary>
        internal CompilerState(string stateFilePath, ITaskFileService taskFileService)
        {
            _cacheInfoList = new String[(int)CompilerStateType.MaxCount];
            _stateFilePath = stateFilePath;
            _taskFileService = taskFileService;
        }

        #region internal methods


        // <summary>
        // Detect whether the state file exists or not. 
        // </summary>
        // <returns></returns>
        internal bool StateFileExists()
        {
            return _taskFileService.Exists(_stateFilePath);
        }

        //
        // Clean up the state file.
        //
        internal void CleanupCache()
        {
            if (StateFileExists() )
            {
                _taskFileService.Delete(_stateFilePath);
            }
        }

        internal bool SaveStateInformation(MarkupCompilePass1 mcPass1)
        {
            Debug.Assert(String.IsNullOrEmpty(_stateFilePath) != true, "StateFilePath must not be empty.");
            Debug.Assert(mcPass1 != null, "A valid instance of MarkupCompilePass1 must be passed to method SaveCacheInformation.");
            Debug.Assert(_cacheInfoList.Length == (int)CompilerStateType.MaxCount, "The Cache string array should be already allocated.");

            // Transfer the cache related information from mcPass1 to this instance.

            AssemblyName = mcPass1.AssemblyName;                  
            AssemblyVersion = mcPass1.AssemblyVersion;
            AssemblyPublicKeyToken = mcPass1.AssemblyPublicKeyToken;
            OutputType = mcPass1.OutputType;
            Language = mcPass1.Language;
            LanguageSourceExtension = mcPass1.LanguageSourceExtension;
            OutputPath = mcPass1.OutputPath;
            RootNamespace = mcPass1.RootNamespace;
            LocalizationDirectivesToLocFile = mcPass1.LocalizationDirectivesToLocFile;
            HostInBrowser = mcPass1.HostInBrowser;
            DefineConstants = mcPass1.DefineConstants;
            ApplicationFile = mcPass1.ApplicationFile;
            PageMarkup = mcPass1.PageMarkupCache;
            ContentFiles = mcPass1.ContentFilesCache;
            SourceCodeFiles = mcPass1.SourceCodeFilesCache;
            References = mcPass1.ReferencesCache;
            PageMarkupFileNames = GenerateStringFromFileNames(mcPass1.PageMarkup);
            SplashImage = mcPass1.SplashImageName;
            Pass2Required = (mcPass1.RequirePass2ForMainAssembly || mcPass1.RequirePass2ForSatelliteAssembly);

            return SaveStateInformation();
        }

        internal bool SaveStateInformation()
        {
            bool bSuccess = false;

            // Save the cache information to the cache file.

            MemoryStream memStream = new MemoryStream();
            // using Disposes the StreamWriter when it ends.  Disposing the StreamWriter 
            // also closes the underlying MemoryStream.  Furthermore, don't add BOM here
            // since TaskFileService.WriteFile adds it.
            using (StreamWriter sw = new StreamWriter(memStream, new UTF8Encoding(false)))
            {
                for (int i =0; i<(int)CompilerStateType.MaxCount; i++)
                {
                    sw.WriteLine(_cacheInfoList[i]);
                }

                sw.Flush();
                _taskFileService.WriteFile(memStream.ToArray(), _stateFilePath);

                bSuccess = true;
            }

            return bSuccess;
        }
        
        //
        // Read the markupcompiler cache file, load the cached information
        // to the corresponding data fields in this class.
        //
        internal bool LoadStateInformation( )
        {
            Debug.Assert(String.IsNullOrEmpty(_stateFilePath) != true, "_stateFilePath must be not be empty.");
            Debug.Assert(_cacheInfoList.Length == (int)CompilerStateType.MaxCount, "The Cache string array should be already allocated.");

            bool loadSuccess = false;

            Stream stream = null;
            if (_taskFileService.IsRealBuild)
            {
                // Pass2 writes to the cache, but Pass2 doesn't have a HostObject (because it's only
                // used for real builds), so it writes directly to the file system. So we need to read
                // directly from the file system; if we read via the HostFileManager, we'll get stale 
                // results that don't reflect updates made in Pass2.
                stream = File.OpenRead(_stateFilePath);
            }
            else
            {
                stream = _taskFileService.GetContent(_stateFilePath);
            }

            // using Disposes the StreamReader when it ends.  Disposing the StreamReader 
            // also closes the underlying MemoryStream.  Don't look for BOM at the beginning
            // of the stream, since we don't add it when writing.  TaskFileService takes care
            // of this.
            using (StreamReader srCache = new StreamReader(stream, false))
            {

                int i = 0;

                while (srCache.EndOfStream != true)
                {
                    if (i >= (int)CompilerStateType.MaxCount)
                    {
                        break;
                    }

                    _cacheInfoList[i] = srCache.ReadLine();

                    i++;
                }

                loadSuccess = true;
            }

            return loadSuccess;

        }

        //
        // Generate cache string for item lists such as PageMarkup, References, 
        // ContentFiles and CodeFiles etc.
        //
        internal static string GenerateCacheForFileList(ITaskItem[] fileItemList)
        {
            string cacheString = String.Empty;

            if (fileItemList != null && fileItemList.Length > 0)
            {
                int iHashCode = 0;

                int iCount = fileItemList.Length;

                for (int i = 0; i < iCount; i++)
                {
                    iHashCode += GetNonRandomizedHashCode(fileItemList[i].ItemSpec);
                }

                StringBuilder sb = new StringBuilder();

                sb.Append(iCount);
                sb.Append(iHashCode);

                cacheString = sb.ToString();
            }


            return cacheString;

        }

        private static string GenerateStringFromFileNames(ITaskItem[] fileItemList)
        {
            string fileNames = String.Empty;

            if (fileItemList != null)
            {
                StringBuilder sb = new StringBuilder();
                
                for (int i = 0; i < fileItemList.Length; i++)
                {
                    sb.Append(fileItemList[i].ItemSpec);
                    sb.Append(";");
                }

                fileNames = sb.ToString();
            }

            return fileNames;
        }

        //
        // Generates a stable hash code value for strings.
        // In .NET Core the hash values for the same string can be different between
        // subsequent program runs and cannot be used for caching here.
        // Copied from String.Comparison.cs 
        //
        private static unsafe int GetNonRandomizedHashCode(string str)
        {
            fixed (char* src = str)
            {
                Debug.Assert(src[str.Length] == '\0', "src[str.Length] == '\\0'");
                Debug.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");

                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                uint* ptr = (uint*)src;
                int length = str.Length;

                while (length > 2)
                {
                    length -= 4;
                    // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                    hash1 = (RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                    ptr += 2;
                }

                if (length > 0)
                {
                    // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ ptr[0];
                }

                return (int)(hash1 + (hash2 * 1566083941));
            }
        }

        private static uint RotateLeft(uint value, int offset)
        {
            return (value << offset) | (value >> (32 - offset));
        }

        #endregion

        #region internal properties

        internal string CacheFilePath
        {
            get { return _stateFilePath; }
        }

        internal string AssemblyName                  
        {
            get { return _cacheInfoList[(int)CompilerStateType.AssemblyName]; }
            set { _cacheInfoList[(int)CompilerStateType.AssemblyName] = value; }
        }

        internal string AssemblyVersion
        {
            get { return _cacheInfoList[(int)CompilerStateType.AssemblyVersion]; }
            set { _cacheInfoList[(int)CompilerStateType.AssemblyVersion] = value; }
        }

        internal string AssemblyPublicKeyToken
        {
            get { return _cacheInfoList[(int)CompilerStateType.AssemblyPublicKeyToken]; }
            set { _cacheInfoList[(int)CompilerStateType.AssemblyPublicKeyToken] = value; }
        }

        internal string OutputType
        {
            get { return _cacheInfoList[(int)CompilerStateType.OutputType]; }
            set { _cacheInfoList[(int)CompilerStateType.OutputType] = value; }
        }

        internal string Language
        {
            get { return _cacheInfoList[(int)CompilerStateType.Language]; }
            set { _cacheInfoList[(int)CompilerStateType.Language] = value; }
        }

        internal string LanguageSourceExtension
        {
            get { return _cacheInfoList[(int)CompilerStateType.LanguageSourceExtension]; }
            set { _cacheInfoList[(int)CompilerStateType.LanguageSourceExtension] = value; }
        }

        internal string OutputPath
        {
            get { return _cacheInfoList[(int)CompilerStateType.OutputPath]; }
            set { _cacheInfoList[(int)CompilerStateType.OutputPath] = value; }
        }

        internal string RootNamespace
        {
            get { return _cacheInfoList[(int)CompilerStateType.RootNamespace]; }
            set { _cacheInfoList[(int)CompilerStateType.RootNamespace] = value; }
        }

        internal string LocalizationDirectivesToLocFile
        {
            get { return _cacheInfoList[(int)CompilerStateType.LocalizationDirectivesToLocFile]; }
            set { _cacheInfoList[(int)CompilerStateType.LocalizationDirectivesToLocFile] = value; }
        }

        internal string HostInBrowser
        {
            get { return _cacheInfoList[(int)CompilerStateType.HostInBrowser]; }
            set { _cacheInfoList[(int)CompilerStateType.HostInBrowser] = value; }
        }

        internal string DefineConstants
        {
            get { return _cacheInfoList[(int)CompilerStateType.DefineConstants]; }
            set { _cacheInfoList[(int)CompilerStateType.DefineConstants] = value; }
        }

        internal string ApplicationFile
        {
            get { return _cacheInfoList[(int)CompilerStateType.ApplicationFile]; }
            set { _cacheInfoList[(int)CompilerStateType.ApplicationFile] = value; }
        }

        internal string PageMarkup
        {
            get { return _cacheInfoList[(int)CompilerStateType.PageMarkup]; }
            set { _cacheInfoList[(int)CompilerStateType.PageMarkup] = value; }
        }
            
        internal string ContentFiles
        {
            get { return _cacheInfoList[(int)CompilerStateType.ContentFiles]; }
            set { _cacheInfoList[(int)CompilerStateType.ContentFiles] = value; }
        }

        internal string SourceCodeFiles
        {
            get { return _cacheInfoList[(int)CompilerStateType.SourceCodeFiles]; }
            set { _cacheInfoList[(int)CompilerStateType.SourceCodeFiles] = value; }
        }

        internal string References
        {
            get { return _cacheInfoList[(int)CompilerStateType.References]; }
            set { _cacheInfoList[(int)CompilerStateType.References] = value; }
        }

        internal string PageMarkupFileNames
        {
            get { return _cacheInfoList[(int)CompilerStateType.PageMarkupFileNames]; }
            set { _cacheInfoList[(int)CompilerStateType.PageMarkupFileNames] = value; }
        }

        internal string SplashImage
        {
            get { return _cacheInfoList[(int)CompilerStateType.SplashImage]; }
            set { _cacheInfoList[(int)CompilerStateType.SplashImage] = value; }
        }

        internal bool Pass2Required
        {
            get { return _cacheInfoList[(int)CompilerStateType.Pass2Required] == bool.TrueString; }
            set { _cacheInfoList[(int)CompilerStateType.Pass2Required] = value.ToString(CultureInfo.InvariantCulture); }
        }

        #endregion

        #region private data

        private String [] _cacheInfoList;
        private string    _stateFilePath;
        private ITaskFileService _taskFileService = null;

        #endregion

    }
}
