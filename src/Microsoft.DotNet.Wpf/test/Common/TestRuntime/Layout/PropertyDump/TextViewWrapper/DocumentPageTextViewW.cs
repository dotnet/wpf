// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;

namespace Microsoft.Test.Layout {
    internal class DocumentPageTextViewW : ReflectionHelper
    {
        public DocumentPageTextViewW(object documentPageTextView):
            base(documentPageTextView, "MS.Internal.Documents.DocumentPageTextView")
        {
        }

        public bool IsValid { 
            get { return (bool)GetProperty("IsValid"); }
        }
        
        public DocumentPageView DocumentPageView {
            get { 
                return (DocumentPageView)GetProperty("DocumentPageView");
            }
        }
        
        public UIElement RenderScope {
            get {
                return (UIElement)GetProperty("RenderScope");
            }
        }

        public TextDocumentViewW TextDocumentViewW
        {
            get
            {
                return new TextDocumentViewW(GetField("_pageTextView"));
            }
        }
        
        public static DocumentPageTextViewW FromIServiceProvider(IServiceProvider serviceProvider) {
            Type textViewType = ReflectionHelper.GetTypeFromName("System.Windows.Documents.ITextView");
            
            if(textViewType == null) {
                throw new ApplicationException("Type System.Windows.Documents.ITextView not found");
            }
                
            object textView = serviceProvider.GetService(textViewType);
            
            if(textView == null) {
                throw new ApplicationException(String.Format("{0} does not provide a service of type {1}", serviceProvider.GetType().ToString(), textViewType.ToString()));
            }
            
            return new DocumentPageTextViewW(textView);
        }
    }
}