// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class FloaterParagraphResultW: ParagraphResultW {
        public FloaterParagraphResultW(object floaterParagraphResult):
            base(floaterParagraphResult, "MS.Internal.Documents.FloaterParagraphResult")
        {
        }
        
        public ParagraphResultListW Paragraphs { 
            get { 
                return new ParagraphResultListW((IEnumerable)GetProperty("FloatingElements"));
            }
        }

        public ColumnResultListW Columns
        {
            get
            {
                return new ColumnResultListW((IEnumerable)GetProperty("Columns"));
            }
        }
    }   
}