// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using System;
using System.IO;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    /// <summary>
    /// Provides data feeds satisfying constraints
    /// </summary>
    public abstract class ConstrainedDataSource 
    {
        /// <summary>
        /// Validate the Constrained Data. Throw if invalid. 
        /// </summary>
        public virtual void Validate() {}

        /// <summary>
        /// Provide an object instance to constraints
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public abstract object GetData(DeterministicRandom r);

        /// <summary>
        /// Get the names of all files under a directory with the patterns defined in filePatterns into a string list. 
        /// </summary>
        /// <param name="dir">Directory</param>
        /// <param name="filePatterns">Search patterns.</param>
        /// <returns></returns>
        protected StringList GetFilesInDirectory(String dir, StringList filePatterns)
        {
            StringList pathList = new StringList();
            foreach (string filePattern in filePatterns)
            {
                pathList.AddRange(Directory.GetFiles(dir, filePattern, SearchOption.AllDirectories));
            }
            return pathList;
        }

        /// <summary>
        /// Verify the path list is not empty and every files in the list exists. If not, throw.
        /// </summary>
        /// <param name="pathList">a list of strings contain the path of the file.</param>
        protected void ValidateFilesExistence(StringList pathList)
        {
            if (pathList.Count == 0)
            {
                throw new FileNotFoundException("pathList is empty.");
            }

            foreach (string path in pathList)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(String.Format("In ConstrainedString: File : {0} not found.", path));
                }
            }
        }
    }
}
