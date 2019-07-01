// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;

namespace Microsoft.Test.Layout {
    internal class LineResultW: ReflectionHelper {
        public LineResultW(object lineResult):
            base(lineResult, "MS.Internal.Documents.LineResult")
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
        
        public double Baseline { 
            get { return (double)GetProperty("Baseline"); }
        }
        
        public OrientedTextPositionW GetTextPositionFromDistance(double distance) {
            return new OrientedTextPositionW(CallMethod("GetTextPositionFromDistance", distance));
        }
            
        public bool IsAtCaretUnitBoundary(TextPointer position) {
            return (bool)CallMethod("IsAtCaretUnitBoundary", position);
        }
        
        public OrientedTextPositionW GetNextCaretUnitPosition(OrientedTextPositionW position, LogicalDirection direction) {
            return new OrientedTextPositionW(CallMethod("GetNextCaretUnitPosition", position.InnerObject, direction));
        }
        
        public OrientedTextPositionW GetBackspaceCaretUnitPosition(OrientedTextPositionW position) {
            return new OrientedTextPositionW(CallMethod("GetBackspaceCaretUnitPosition", position.InnerObject));
        }
        
        public ReadOnlyCollection<GlyphRun> GetGlyphRuns(TextPointer start, TextPointer end) {
            return (ReadOnlyCollection<GlyphRun>)CallMethod("GetGlyphRuns", start, end);
        }

        public TextPointer GetContentEndPosition() {
            return (TextPointer)CallMethod("GetContentEndPosition");
        }
        
        public TextPointer GetEllipsesPosition() {
            return (TextPointer)CallMethod("GetEllipsesPosition");
        }
    }   
}