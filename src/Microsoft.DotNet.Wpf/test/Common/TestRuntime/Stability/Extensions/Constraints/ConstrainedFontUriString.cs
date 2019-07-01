// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedFontUriString : ConstrainedString
    {

        protected override string TransformString(string source)
        {
            if(source.StartsWith(comment))
            {
                return String.Empty;
            }

            string[] pair = source.Split(separator);
            return "file:///" + pair[0].Trim() + "\\#" + pair[1].Trim();

        }

        private string comment = "#";
        private char separator = '|';
    }
}
