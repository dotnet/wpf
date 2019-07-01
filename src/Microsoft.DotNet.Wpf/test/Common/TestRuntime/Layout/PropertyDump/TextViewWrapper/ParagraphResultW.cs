// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class ParagraphResultW: ReflectionHelper {
        public ParagraphResultW(object paragraphResult):
            this(paragraphResult, "MS.Internal.Documents.ParagraphResult")
        {
        }
        
        protected ParagraphResultW(object paragraphResult, string typeName):
            base(paragraphResult, typeName)
        {
        }
        
        public TextPointer StartPosition { 
            get { return (TextPointer)GetProperty("StartPosition"); } 
        }          
        
        public TextPointer EndPosition { 
            get { return (TextPointer)GetProperty("EndPosition"); } 
        }
        
        public Rect LayoutBox { 
            get { return (Rect)GetProperty("LayoutBox"); }
        }
        
        public Rect GetRectangleFromTextPosition(OrientedTextPositionW position) {
            return (Rect)CallMethod("GetRectangleFromTextPosition", position.InnerObject); 
        }
        
        public static ParagraphResultW FromObject(object o) {
            if(0 == String.Compare(o.GetType().ToString(), "MS.Internal.Documents.ContainerParagraphResult")) {
                return new ContainerParagraphResultW(o);
            }
            if(0 == String.Compare(o.GetType().ToString(), "MS.Internal.Documents.TextParagraphResult")) {
                return new TextParagraphResultW(o);
            }
            if(0 == String.Compare(o.GetType().ToString(), "MS.Internal.Documents.TableParagraphResult")) {
                return new TableParagraphResultW(o);
            }
            if(0 == String.Compare(o.GetType().ToString(), "MS.Internal.Documents.UIElementParagraphResult")) {
                return new UIElementParagraphResultW(o);
            }
            if(0 == String.Compare(o.GetType().ToString(), "MS.Internal.Documents.SubpageParagraphResult")) {
                return new SubpageParagraphResultW(o);
            }
            if (0 == String.Compare(o.GetType().ToString(), "MS.Internal.Documents.FloaterParagraphResult"))
            {
                return new FloaterParagraphResultW(o);
            }
            if (0 == String.Compare(o.GetType().ToString(), "MS.Internal.Documents.FigureParagraphResult"))
            {
                return new FigureParagraphResultW(o);
            }
            if (0 == String.Compare(o.GetType().ToString(), "MS.Internal.Documents.RowParagraphResult"))
            {
                return new RowParagraphResultW(o);
            }

            throw new ApplicationException(String.Format("Unknown paragraphresult of type {0}", o.GetType()));          
        }
    }
}