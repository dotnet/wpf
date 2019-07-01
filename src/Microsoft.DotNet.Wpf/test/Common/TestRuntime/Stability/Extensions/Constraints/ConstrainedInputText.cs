// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedInputText : ConstrainedDataSource
    {
        public ConstrainedInputText()
        {
        }

        public override void Validate()
        {
            if (pathList == null)
            {
                pathList = GetFilesInDirectory(Dir, FilePatterns);
            }

            ValidateFilesExistence(pathList);
        }

        /// <summary>
        /// Read the content of the files located in the DIR directory and break it up into paragraphs.
        /// </summary>
        /// <returns>String array of paragraphs</returns>
        public override object GetData(DeterministicRandom r)
        {
            if (paragraphs == null)
            {
                int capacity = (int)GetTotalSize(pathList);
                StringBuilder inputText = new StringBuilder(capacity);
                foreach (string path in pathList)
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        inputText.Append(sr.ReadToEnd());
                    }
                }

                paragraphs = inputText.ToString().Split(Environment.NewLine.ToCharArray());
            }

            return paragraphs;
        }

        private long GetTotalSize(StringList pathList)
        {
            long totalSize = 0;
            foreach (string path in pathList)
            {
                FileInfo fi = new FileInfo(path);
                totalSize += fi.Length;
            }
            return totalSize;
        }

        private string[] paragraphs = null;
        public String Dir { get; set; }
        public StringList FilePatterns { get; set; }
        StringList pathList = null;
    }
}
