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
    internal class TextParagraphResultW : ParagraphResultW
    {
        public TextParagraphResultW(object textParagraphResult):
            base(textParagraphResult, "MS.Internal.Documents.TextParagraphResult")
        {
        }
        
        public LineResultListW Lines { 
            get { 
                IEnumerable lines = (IEnumerable)GetProperty("Lines");
                return (lines == null) ? null : new LineResultListW(lines);
            } 
        }

        public ParagraphResultListW Floaters
        { 
            get {
                if ((IEnumerable)GetProperty("Floaters") != null)
                    return new ParagraphResultListW((IEnumerable)GetProperty("Floaters"));
                else
                    return null;
            } 
        }

        public ParagraphResultListW Figures
        {
            get
            {
                if ((IEnumerable)GetProperty("Figures") != null)
                    return new ParagraphResultListW((IEnumerable)GetProperty("Figures"));
                else
                    return null;
            } 
        }
    }   
}