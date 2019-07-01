// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class TextParagraphViewW : ReflectionHelper
    {
        public TextParagraphViewW(object textParagraphView):
            base(textParagraphView, "MS.Internal.Documents.TextParagraphView")
        {
        }
        
        public LineResultListW Lines { 
            get { 
                IEnumerable lines = (IEnumerable)GetProperty("Lines");
                return new LineResultListW(lines); 
            } 
        }

        public static TextParagraphViewW FromIServiceProvider(IServiceProvider serviceProvider)
        {
            Type textViewType = ReflectionHelper.GetTypeFromName("System.Windows.Documents.ITextView");

            
            if(textViewType == null) {
                throw new ApplicationException("Type System.Windows.Documents.ITextView not found");
            }
                
            object textView = serviceProvider.GetService(textViewType);
            
            if(textView == null) {
                throw new ApplicationException(String.Format("{0} does not provide a service of type {1}", serviceProvider.GetType().ToString(), textViewType.ToString()));
            }
            
            /*
            if(!textView.IsValid) {
                textView.Validate();
            }
            */
            
            return new TextParagraphViewW(textView);
        }
    }
}