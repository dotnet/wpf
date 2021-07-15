// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description:
//   an internal class that keeps the related information for a source file.
//   Such as the relative path, source directory, Link path and 
//   file stream etc.
//
//   This can be shared by different build tasks.
//
//---------------------------------------------------------------------------

using System;
using System.IO;

namespace MS.Internal
{
    #region SourceFileInfo

    // <summary>
    // SourceFileInfo class
    // </summary>
    internal class SourceFileInfo 
    {
    #region Constructor
        // <summary>
        // Constructor
        // </summary>
        internal SourceFileInfo(FileUnit file)
        {
            _filePath = file.Path;
            _fileLinkAlias = file.LinkAlias;
            _fileLogicalName = file.LogicalName;
            _sourcePath = null;
            _relativeSourceFilePath = null;

            _stream = null;

            _isXamlFile = false;

            if (!string.IsNullOrEmpty(file.Path) && file.Path.ToUpperInvariant().EndsWith(XAML, StringComparison.Ordinal))
            {
                _isXamlFile = true;
            }
        }

    #endregion Constructor

    #region Properties

        // 
        // The original file Path
        // 
        internal string OriginalFilePath
        {
            get { return _filePath; }
        }

        // 
        // The original file LinkAlias
        // 
        internal string OriginalFileLinkAlias
        {
            get { return _fileLinkAlias; }
        }

        // 
        // The original file LogicalName
        // 
        internal string OriginalFileLogicalName
        {
            get { return _fileLogicalName; }
        }

        // 
        // The new Source Directory for this filepath
        //
        // If the file is under the project root, this is the project root directory,
        // otherwise, this is the directory of the file.
        // 
        internal string SourcePath
        {
            get { return _sourcePath;  }
            set { _sourcePath = value; }
        }

        // 
        // The new relative path which is relative to the SourcePath.
        // 
        // If it is XamlFile,  the RelativeSourceFilePath would not include the .xaml extension.
        //
        internal string RelativeSourceFilePath
        {
            get { return _relativeSourceFilePath; }
            set { _relativeSourceFilePath = value; }
        }

        //
        // Indicate if the source file is a xaml file or not.
        //
        internal bool IsXamlFile
        {
            get { return _isXamlFile; }
        }

        //
        // Stream of the file
        //
        internal Stream Stream
        {
            get 
            {
                //
                // If the stream is not set for the file, get it from file system in Disk.
                //
                if ( _stream == null)
                {
                    _stream = File.OpenRead(_filePath);
                }

                return _stream;
            }

            set
            { 
                _stream = value; 
            }
        }

    #endregion Properties

    #region internal methods

        //
        // Close the stream.
        //
        internal void CloseStream()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
        }

    #endregion

    #region Private Data

        private string _filePath;
        private string _fileLinkAlias;
        private string _fileLogicalName;
        private string _sourcePath;
        private string _relativeSourceFilePath;
        private Stream _stream;
        private bool   _isXamlFile;

        private const string XAML = ".XAML";

    #endregion Private Data

    }
    #endregion SourceFileInfo

}
