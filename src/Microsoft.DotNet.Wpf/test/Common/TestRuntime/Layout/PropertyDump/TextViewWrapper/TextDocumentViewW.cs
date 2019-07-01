// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class TextDocumentViewW : ReflectionHelper
    {
        public TextDocumentViewW(object textDocumentView):
            base(textDocumentView, "MS.Internal.Documents.TextDocumentView")
        {
        }

        public bool IsValid { 
            get { return (bool)GetProperty("IsValid"); }
        }
        
        public ColumnResultListW Columns { 
            get { 
                IEnumerable columns = (IEnumerable)GetProperty("Columns");                
                return (columns == null) ? null : new ColumnResultListW(columns); 
            } 
        }
        
        public static TextDocumentViewW FromIServiceProvider(IServiceProvider serviceProvider) {
            Type textViewType = ReflectionHelper.GetTypeFromName("System.Windows.Documents.ITextView");
            
            if(textViewType == null) {
                throw new ApplicationException("Type System.Windows.Documents.ITextView not found");
            }
                
            object textView = serviceProvider.GetService(textViewType);
            
            if(textView == null) {
                throw new ApplicationException(String.Format("{0} does not provide a service of type {1}", serviceProvider.GetType().ToString(), textViewType.ToString()));
            }
            
            return new TextDocumentViewW(textView);
        }
    }
}