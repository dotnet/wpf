// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class RowParagraphResultW: ParagraphResultW {
        public RowParagraphResultW(object RowParagraphResult):
            base(RowParagraphResult, "MS.Internal.Documents.RowParagraphResult")
        {
        }
        
        public ParagraphResultListW CellParagraphs { 
            get { 
                return new ParagraphResultListW((IEnumerable)GetProperty("CellParagraphs"));
            }
        }
        
    }   
}