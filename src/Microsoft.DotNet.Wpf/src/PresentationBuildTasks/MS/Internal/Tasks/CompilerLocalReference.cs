// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//----------------------------------------------------------------------------------------
// 
// Description:
//       this internal class handles cache for xaml files which 
//       want to reference local types.
//
//---------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Build.Tasks.Windows;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MS.Utility;

namespace MS.Internal.Tasks
{
    // <summary>
    // LocalReferenceFile
    // This class keeps xaml file path and whether it is localizable.
    // </summary>
    internal class LocalReferenceFile
    {
        private bool   _localizable;
        private string _filePath;
        private string _linkAlias;
        private string _logicalName;
        private static LocalReferenceFile _empty = new LocalReferenceFile(String.Empty, false, String.Empty, String.Empty);
        private const char trueChar  = 'T';
        private const char falseChar = 'F';
        private const char semiColonChar = ';';

        internal LocalReferenceFile(string filepath, bool localizable, string linkAlias, string logicalName)
        {
            _localizable = localizable;
            _filePath   = filepath;
            _linkAlias = linkAlias;
            _logicalName = logicalName;
        }

        internal bool Localizable
        {
            get { return _localizable; }
        }

        internal string FilePath
        {
            get { return _filePath; }
        }

        internal string LinkAlias
        {
            get { return _linkAlias; }
        }

        internal string LogicalName
        {
            get { return _logicalName; }
        }

        internal static LocalReferenceFile Empty
        {
            get { return _empty; }
        }

        // 
        // Serialize the instance to a string so that it can saved into a cache file.
        //
        internal string Serialize()
        {
            string cacheText = String.Empty;

            if (!String.IsNullOrEmpty(FilePath))
            {
                StringBuilder sb = new StringBuilder();

                if (Localizable)
                {
                    sb.Append(trueChar);
                }
                else
                {
                    sb.Append(falseChar);
                }

                sb.Append(FilePath);
                sb.Append(semiColonChar);
                sb.Append(LinkAlias);
                sb.Append(semiColonChar);
                sb.Append(LogicalName);

                cacheText = sb.ToString();
            }

            return cacheText;
        }

        //
        // Create instance from a cache text string.
        //
        internal static LocalReferenceFile Deserialize(string cacheInfo)
        {
            // cachInfo string must follow pattern like Localizable + FilePath.
            // Localizable contains one character.
            LocalReferenceFile lrf = null;

            if (!String.IsNullOrEmpty(cacheInfo))
            {
                bool  localizable;
                string filePath;
                string linkAlias;
                string logicalName;

                string[] subStrs = cacheInfo.Split(semiColonChar);

                filePath = subStrs[0];
                linkAlias = subStrs[1];
                logicalName = subStrs[2];

                localizable = (filePath[0] == trueChar) ? true : false;

                filePath = filePath.Substring(1);

                lrf = new LocalReferenceFile(filePath, localizable, linkAlias, logicalName);
            }

            return lrf;
        }
    }

    // <summary>
    // CompilerLocalReference
    // </summary>
    internal class CompilerLocalReference  
    {
        // <summary>
        // ctor of CompilerLocalReference
        // </summary>
        internal CompilerLocalReference(string localCacheFile, ITaskFileService taskFileService)
        {
            _localCacheFile = localCacheFile;
            _taskFileService = taskFileService;
        }

        #region internal methods

        // <summary>
        // Detect whether the local cache file exists or not. 
        // </summary>
        // <returns></returns>
        internal bool CacheFileExists()
        {
            return _taskFileService.Exists(_localCacheFile);
        }

        //
        // Clean up the state file.
        //
        internal void CleanupCache()
        {
            if (CacheFileExists())
            {
                _taskFileService.Delete(_localCacheFile);
            }
        }

        //
        // Save the local reference related information  from MarkupCompilePass1 task to cache file.
        //
        internal bool SaveCacheInformation(MarkupCompilePass1 mcPass1)
        {
            Debug.Assert(String.IsNullOrEmpty(_localCacheFile) != true, "_localCacheFile must not be empty.");
            Debug.Assert(mcPass1 != null, "A valid instance of MarkupCompilePass1 must be passed to method SaveCacheInformation.");

            bool bSuccess = false;

            // Transfer the cache related information from mcPass1 to this instance.

            LocalApplicationFile = mcPass1.LocalApplicationFile;
            LocalMarkupPages = mcPass1.LocalMarkupPages;

            // Save the cache information to the cache file.

            MemoryStream memStream = new MemoryStream();
            
            // using Disposes the StreamWriter when it ends.  Disposing the StreamWriter 
            // also closes the underlying MemoryStream.  Furthermore, don't add BOM here
            // since TaskFileService.WriteFile adds it.
            using (StreamWriter sw = new StreamWriter(memStream, new UTF8Encoding(false)))
            {
                // Write InternalTypeHelperFile only when Pass1 asks Pass2 to do further check.
                if (mcPass1.FurtherCheckInternalTypeHelper)
                {
                    sw.WriteLine(mcPass1.InternalTypeHelperFile);
                }
                else
                {
                    sw.WriteLine(String.Empty);
                }

                // Write the ApplicationFile for Pass2 compilation.
                if (LocalApplicationFile == null)
                {
                    sw.WriteLine(String.Empty);
                }
                else
                {
                    sw.WriteLine(LocalApplicationFile.Serialize());
                }

                if (LocalMarkupPages != null && LocalMarkupPages.Length > 0)
                {
                    for (int i = 0; i < LocalMarkupPages.Length; i++)
                    {
                        sw.WriteLine(LocalMarkupPages[i].Serialize( ));
                    }
                }

                sw.Flush();
                _taskFileService.WriteFile(memStream.ToArray(), _localCacheFile);

                bSuccess = true;
            }

            return bSuccess;
        }
        
        //
        // Read the Local Reference cache file, load the cached information
        // to the corresponding data fields in this class.
        //
        internal bool LoadCacheFile()
        {
            Debug.Assert(String.IsNullOrEmpty(_localCacheFile) != true, "_localCacheFile must not be empty.");

            bool loadSuccess = false;

            Stream stream = _taskFileService.GetContent(_localCacheFile);

            // using Disposes the StreamReader when it ends.  Disposing the StreamReader 
            // also closes the underlying MemoryStream.  Don't look for BOM at the beginning
            // of the stream, since we don't add it when writing.  TaskFileService takes care
            // of this.
            using (StreamReader srCache = new StreamReader(stream, false))
            {
                // The first line must be for InternalTypeHelperFile.
                // The second line is for Local Application Defintion file.
                // For Library, the second line is an empty line.

                InternalTypeHelperFile = srCache.ReadLine();

                string lineText;

                lineText = srCache.ReadLine();

                LocalApplicationFile = LocalReferenceFile.Deserialize(lineText);

                ArrayList alMarkupPages = new ArrayList();

                while (srCache.EndOfStream != true)
                {
                    lineText = srCache.ReadLine();
                    LocalReferenceFile lrf = LocalReferenceFile.Deserialize(lineText);

                    if (lrf != null)
                    {
                        alMarkupPages.Add(lrf);
                    }
                }

                if (alMarkupPages.Count > 0)
                {
                    LocalMarkupPages = (LocalReferenceFile []) alMarkupPages.ToArray(typeof(LocalReferenceFile));
                }

                loadSuccess = true;
            }

            return loadSuccess;

        }

        #endregion

        #region internal properties

        internal string CacheFilePath
        {
            get { return _localCacheFile; }
        }

        internal  LocalReferenceFile LocalApplicationFile
        {
            get { return _localApplicationFile; }
            set { _localApplicationFile = value; }
        }

        internal LocalReferenceFile[] LocalMarkupPages
        {
            get { return _localMarkupPages; }
            set { _localMarkupPages = value; }
        }

        //
        // InternalTypeHelper file path.
        //
        // Since Pass2 task doesn't know the code language, it could not generate the real 
        // InternalTypeHelper code file path on the fly.
        // The real file path would be passed from Pass1 task to Pass2 through the cache file.
        //
        // If this file path is set in the cache file, it means Pass1 asks Passs2 to do the further
        // check for this file to see if it is really required, if this file is required for the assembly,
        // Pass2 then adds it to the appropriate output Item, otherwise, Pass2 just deletes this file.
        //
        // If the path is empty, this means Pass1 has already known how to handle the file correctly, 
        // no further check is required in Pass2.
        //
        internal string InternalTypeHelperFile
        {
            get { return _internalTypeHelperFile; }
            set { _internalTypeHelperFile = value; }
        }

        #endregion

        #region private data

        private LocalReferenceFile    _localApplicationFile;
        private LocalReferenceFile[]  _localMarkupPages;

        private string                _localCacheFile;
        private string                _internalTypeHelperFile = String.Empty;
        private ITaskFileService      _taskFileService = null;

        #endregion

    }
}
