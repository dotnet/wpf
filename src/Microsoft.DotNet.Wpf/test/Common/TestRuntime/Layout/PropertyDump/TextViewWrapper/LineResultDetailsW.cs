// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class LineResultDetailsW: ReflectionHelper {
        public LineResultDetailsW(object lineResultDetails):
            base(lineResultDetails, "MS.Internal.Documents.LineResultDetails")
        {
        }
        
        public TextPointer ContentEndPosition { 
            get { return (TextPointer)GetProperty("ContentEndPosition"); }
        }
        
        public TextPointer EllipsesPosition { 
            get { return (TextPointer)GetProperty("EllipsesPosition"); }
        }     

        public bool HasEllipses { 
            get { return (bool)GetProperty("HasEllipses"); } 
        }
    }   
}