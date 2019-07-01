// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Randomly highlight some Text of FlowDocument.
    /// </summary>
    public class SelectFlowDocumentTextAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FlowDocument FlowDocument { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            parent = FlowDocument.Parent as FrameworkElement;
            return parent == null;
        }

        public override void Perform()
        {
            parent.Focus();
            BindingFlags STATIC_INSTANCE_BINDING = BindingFlags.Static | BindingFlags.NonPublic;
            Type textEditorType = typeof(FrameworkElement).Assembly.GetType("System.Windows.Documents.TextEditor");
            MethodInfo mi = textEditorType.GetMethod("GetTextSelection", STATIC_INSTANCE_BINDING);
            TextSelection ts = mi.Invoke(null, new object[] { parent }) as TextSelection;
            TextRange subRange = GetRandomSubRange(FlowDocument.ContentStart, FlowDocument.ContentEnd);
            if (ts != null)
            {
                ts.Select(subRange.Start, subRange.End);
            }
        }

        #endregion

        #region Private Members;

        private FrameworkElement parent = null;

        private TextRange GetRandomSubRange(TextPointer start, TextPointer end)
        {
            TextPointer subStart = start.GetPositionAtOffset(StartIndex % start.GetOffsetToPosition(end));
            TextPointer subEnd = start.GetPositionAtOffset(EndIndex % subStart.GetOffsetToPosition(end));
            return new TextRange(subStart, subEnd);
        }

        #endregion
    }
}
