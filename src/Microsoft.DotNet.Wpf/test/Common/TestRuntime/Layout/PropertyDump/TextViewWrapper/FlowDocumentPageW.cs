// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Collections;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.Test.Layout {
    internal class FlowDocumentPageW: ReflectionHelper {
        public FlowDocumentPageW(object flowDocumentPage):
            base(flowDocumentPage, "MS.Internal.PtsHost.FlowDocumentPage")
        {
        }
        
        public int FormattedLinesCount { 
            get { return (int)GetProperty("FormattedLinesCount"); } 
        }          
        
        public ColumnResultListW GetColumnResults() {
            return new ColumnResultListW((IEnumerable)CallMethod("GetColumnResults"));
        }
    }
}