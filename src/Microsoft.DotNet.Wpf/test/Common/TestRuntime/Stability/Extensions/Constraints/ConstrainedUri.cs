// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Test.Stability.Core;


namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class UriList : List<Uri> { }

    public class ConstrainedUri : ConstrainedDataSource
    {
        public ConstrainedUri()
        {
        }

        public override object GetData(DeterministicRandom r)
        {
            if (needToAddFilesWithSearchPatterns)
            {
                UriList urisFromPathList = MakeUris(pathList);

                if (Uris == null)
                {
                    Uris = urisFromPathList;
                }
                else
                {
                    Uris.AddRange(MakeUris(pathList));
                }

                needToAddFilesWithSearchPatterns = false;
            }

            return r.NextItem<Uri>(Uris);
        }

        public override void Validate()
        {
            if ((Dir == null) || (FilePatterns == null))
            {
                needToAddFilesWithSearchPatterns = false;
            }
            else
            {
                if (pathList == null)
                {
                    pathList = GetFilesInDirectory(Dir, FilePatterns);
                }

                ValidateFilesExistence(pathList);
            }
        }

        private UriList MakeUris(StringList paths)
        {
            UriList newUris = new UriList();
            foreach (string s in paths)
            {
                newUris.Add(new Uri(s));
            }
            return newUris;
        }

        public UriList Uris { get; set; }
        public String Dir { get; set; }
        public StringList FilePatterns { get; set; }
        private StringList pathList = null;
        private bool needToAddFilesWithSearchPatterns = true;
    }
}
