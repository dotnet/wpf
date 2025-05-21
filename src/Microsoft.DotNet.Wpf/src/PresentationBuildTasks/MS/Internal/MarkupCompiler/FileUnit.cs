// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MS.Internal
{
    ///<summary>
    /// The FileUnit class
    ///</summary> 
    [Serializable]
    internal struct FileUnit
    {
        public FileUnit(string path, string linkAlias, string logicalName)
        {
            _path = path;
            _linkAlias = linkAlias;
            _logicalName = logicalName;
        }
        
        public string Path 
        { 
            get { return _path; }
        }
        
        public string LinkAlias 
        { 
            get { return _linkAlias; }
        }

        public string LogicalName 
        { 
            get { return _logicalName; }
        }

        public static FileUnit Empty
        {
            get { return _empty; }
        }

        public override string ToString()
        {
            return _path;
        }

        private string _path;
        private string _linkAlias;
        private string _logicalName;
        
        private static FileUnit _empty = new FileUnit(String.Empty, String.Empty, String.Empty);
    }
}

