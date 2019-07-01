// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class TableParagraphResultW : ParagraphResultW
    {
        public TableParagraphResultW(object tableParagraphResult):
            base(tableParagraphResult, "MS.Internal.Documents.TableParagraphResult")
        {
        }
                
        public ParagraphResultListW Paragraphs
        {
            get
            {
                return new ParagraphResultListW((IEnumerable)GetProperty("Paragraphs"));
            }
        }
    }   
}