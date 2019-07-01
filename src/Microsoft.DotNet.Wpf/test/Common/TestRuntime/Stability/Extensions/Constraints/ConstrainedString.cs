// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class StringList : List<String> { }
    public class ConstrainedString : ConstrainedDataSource
    {
        public override object GetData(DeterministicRandom r)
        {
            if (strings == null)
            {
                ConstructStringsList();
            }

            return r.NextItem<String>(strings);
        }

        public override void Validate()
        {
            if (pathList == null)
            {
                pathList = GetFilesInDirectory(Dir, FilePatterns);
            }

            ValidateFilesExistence(pathList);
        }

        private void ConstructStringsList()
        {
            strings = new List<String>();

            foreach (string path in pathList)
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string strLine = sr.ReadLine();

                    while (strLine != null)
                    {                        
                        strLine = TransformString(strLine);

                        if(!String.IsNullOrEmpty(strLine))
                        {
                            strings.Add(strLine);
                        }

                        strLine = sr.ReadLine();
                    }
                }
            }
        }

        protected virtual string TransformString(string source)
        {
            return source;
        }

        public String Dir { get; set; }
        public StringList FilePatterns { get; set; }

        private StringList pathList = null;
        private List<String> strings = null;
    }
}
