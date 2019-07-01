// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class UIElementParagraphResultW: ParagraphResultW {
        public UIElementParagraphResultW(object uIElementParagraphResult):
            base(uIElementParagraphResult, "MS.Internal.Documents.UIElementParagraphResult")
        {
        }
        
        public DependencyObject Element {
            get { return (DependencyObject)GetProperty("Element"); } 
        }
    }   
}