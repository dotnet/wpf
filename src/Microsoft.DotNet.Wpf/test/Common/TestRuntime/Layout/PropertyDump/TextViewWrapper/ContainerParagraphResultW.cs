// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class ContainerParagraphResultW: ParagraphResultW {
        public ContainerParagraphResultW(object containerParagraphResult):
            base(containerParagraphResult, "MS.Internal.Documents.ContainerParagraphResult")
        {
        }
        
        public ParagraphResultListW Paragraphs { 
            get { 
                return new ParagraphResultListW((IEnumerable)GetProperty("Paragraphs"));
            }
        }
    }   
}