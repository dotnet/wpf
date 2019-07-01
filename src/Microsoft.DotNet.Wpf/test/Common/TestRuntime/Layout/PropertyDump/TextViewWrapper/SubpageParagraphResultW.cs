// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Reflection;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class SubpageParagraphResultW : ParagraphResultW
    {
        public SubpageParagraphResultW(object subpageParagraphResult):
            base(subpageParagraphResult, "MS.Internal.Documents.SubpageParagraphResult")
        {
        }
        
        public ColumnResultListW Columns { 
            get { 
                IEnumerable columns = (IEnumerable)GetProperty("Columns");
                return (columns == null) ? null : new ColumnResultListW(columns);
            }
        }
    }   
}