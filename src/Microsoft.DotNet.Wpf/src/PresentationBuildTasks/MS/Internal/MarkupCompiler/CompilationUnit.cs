// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MS.Internal
{
    ///<summary>
    /// The CompilationUnit class
    ///</summary> 
    internal class CompilationUnit 
    {
#region Constructors

        ///<summary>constructor</summary> 
        public CompilationUnit(string assemblyName, string language, string defaultNamespace, FileUnit[] fileList)
        {
            _assemblyName = assemblyName;
            _language = language;
            _fileList = fileList;
            _defaultNamespace = defaultNamespace;
        }

#endregion Constructors

#region Properties

        internal bool Pass2
        {
            get { return _pass2; }
            set { _pass2 = value; }
        }

        ///<summary>Name of the assembly the package is compiled into</summary> 
        public string AssemblyName
        {
            get { return _assemblyName; }
        }
        
        ///<summary>Name of the CLR language the package is compiled into</summary> 
        public string Language
        {
            get { return _language; }
        }

        ///<summary>path to the project root</summary> 
        public string SourcePath
        {
            get { return _sourcePath; }
            set { _sourcePath = value; }
        }
        
        ///<summary>Default CLR Namespace of the project</summary> 
        public string DefaultNamespace
        {
            get { return _defaultNamespace; }
        }

        ///<summary>Application definition file (relative to SourcePath) </summary> 
        public FileUnit ApplicationFile
        {
            get { return _applicationFile; }
            set { _applicationFile = value; }
        }

        ///<summary>A list of relative (to SourcePath) file path names comprising the package to be compiled</summary> 
        public FileUnit[] FileList
        {
            get { return _fileList; }
        }

#endregion Properties

#region Private Data

        private bool                    _pass2 = false;
        private string                  _assemblyName = string.Empty;
        private string                  _language = string.Empty;
        private string                  _sourcePath = string.Empty;
        private string                  _defaultNamespace = string.Empty;
        private FileUnit                _applicationFile = FileUnit.Empty;
        private FileUnit[]              _fileList = null;

#endregion Private Data
    }

#region ErrorEvent

    /// <summary>
    /// Delegate for the Error event. 
    /// </summary>
    internal delegate void MarkupErrorEventHandler(Object sender, MarkupErrorEventArgs e);

    /// <summary>
    /// Event args for the Error event
    /// </summary>
    internal class MarkupErrorEventArgs : EventArgs
    {
#region Constructors

        /// <summary>
        /// constructor
        /// </summary>
        internal MarkupErrorEventArgs(Exception e, int lineNum, int linePos, string fileName)
        {
            _e = e;
            _lineNum = lineNum;
            _linePos = linePos;
            _fileName = fileName;
        }

#endregion Constructors

#region Properties

        /// <summary>
        /// The Error Message
        /// </summary>
        public Exception Exception
        {
            get { return _e; }
        }
        
        /// <summary>
        /// The line number at which the compile Error occured
        /// </summary>
        public int LineNumber
        {
            get { return _lineNum; }
        }
        
        /// <summary>
        /// The character position in the line at which the compile Error occured
        /// </summary>
        public int LinePosition
        {
            get { return _linePos; }
        }

        /// <summary>
        /// The xaml file in which the compile Error occured
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
        }

#endregion Properties

#region Private Data

        private int _lineNum;
        private int _linePos;
        private Exception _e;
        private string _fileName;

#endregion Private Data

    }

#endregion ErrorEvent


#region SourceFileResolveEvent

    /// <summary>
    /// Delegate for the SourceFileResolve Event. 
    /// </summary>
    internal delegate void SourceFileResolveEventHandler(Object sender, SourceFileResolveEventArgs e);

    /// <summary>
    /// Event args for the Error event
    /// </summary>
    internal class SourceFileResolveEventArgs: EventArgs
    {
#region Constructors

        /// <summary>
        /// constructor
        /// </summary>
        internal SourceFileResolveEventArgs(FileUnit file)
        {
            _sourceFileInfo = new SourceFileInfo(file);
        }

#endregion Constructors

#region Properties

        //
        // FileInfo 
        //
        internal SourceFileInfo SourceFileInfo
        {
            get { return _sourceFileInfo; }
        }

#endregion Properties

#region Private Data

        private SourceFileInfo _sourceFileInfo;

#endregion Private Data

    }

#endregion SourceFileResolveEvent


#region ReferenceAssembly

    // <summary>
    // Reference Assembly
    // Passed by CodeGenertation Task.
    // Consumed by Parser.
    // </summary>
    internal class ReferenceAssembly : MarshalByRefObject
    {
    #region Constructor
        // <summary>
        // Constructor
        // </summary>
        internal ReferenceAssembly()
        {
            _path = null;
            _assemblyName = null;
        }
        // <summary>
        // Constructor
        // </summary>
        // <param name="path"></param>
        // <param name="assemblyname"></param>
        internal ReferenceAssembly(string path, string assemblyname)
        {
            Path = path;
            AssemblyName = assemblyname;
        }

    #endregion Constructor

    #region Internal Properties

        // <summary>
        // The path for the assembly.
        // The path must end with "\", but not include any Assembly Name.
        // </summary>
        // <value></value>
        internal string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        // <summary>
        // AssemblyName without any Extension part.
        // </summary>
        // <value></value>
        internal string AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value;}
        }        

    #endregion Internal Properties

    #region private fields

        private string _path;
        private string _assemblyName;

    #endregion private fields

    }
#endregion ReferenceAssembly

}
