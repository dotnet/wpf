// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: AutomationPeer associated with FlowDocumentReader.
//

using System.Collections.Generic;           // List<T>
using System.Windows.Automation.Provider;   // IMultipleViewProvider
using System.Windows.Controls;              // FlowDocumentReader
using System.Windows.Documents;             // FlowDocument
using MS.Internal;                          // Invariant

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer associated with FlowDocumentScrollViewer.
    /// </summary>
    public class FlowDocumentReaderAutomationPeer : FrameworkElementAutomationPeer, IMultipleViewProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public FlowDocumentReaderAutomationPeer(FlowDocumentReader owner)
            : base(owner)
        { }

        /// <summary>
        /// <see cref="AutomationPeer.GetPattern"/>
        /// </summary>
        public override object GetPattern(PatternInterface patternInterface)
        {
            object returnValue = null;
            if (patternInterface == PatternInterface.MultipleView)
            {
                returnValue = this;
            }
            else
            {
                returnValue = base.GetPattern(patternInterface);
            }
            return returnValue;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetChildrenCore"/>
        /// </summary>
        /// <remarks>
        /// AutomationPeer associated with FlowDocumentScrollViewer returns an AutomationPeer
        /// for hosted Document and for elements in the style.
        /// </remarks>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            // Get children for all elements in the style.
            List<AutomationPeer> children = base.GetChildrenCore();

            // Add AutomationPeer associated with the document.
            // Make it the first child of the collection.
            FlowDocument document = ((FlowDocumentReader)Owner).Document;
            if (document != null)
            {
                AutomationPeer documentPeer = ContentElementAutomationPeer.CreatePeerForElement(document);
                if (_documentPeer != documentPeer)
                {
                    if (_documentPeer != null)
                    {
                        _documentPeer.OnDisconnected();
                    }
                    _documentPeer = documentPeer as DocumentAutomationPeer;
                }
                if (documentPeer != null)
                {
                    if (children == null)
                    {
                        children = new List<AutomationPeer>();
                    }
                    children.Add(documentPeer);
                }
            }

            return children;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClassNameCore"/>
        /// </summary>
        protected override string GetClassNameCore()
        {
            return "FlowDocumentReader";
        }

        /// <summary>
        /// This helper synchronously fires automation PropertyChange event
        /// in responce to current view mode change.
        /// </summary>
        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseCurrentViewChangedEvent(FlowDocumentReaderViewingMode newMode, FlowDocumentReaderViewingMode oldMode)
        {
            if (newMode != oldMode)
            {
                RaisePropertyChangedEvent(MultipleViewPatternIdentifiers.CurrentViewProperty,
                    ConvertModeToViewId(newMode), ConvertModeToViewId(oldMode));
            }
        }

        /// <summary>
        /// This helper synchronously fires automation PropertyChange event
        /// in responce to supported views change.
        /// </summary>
        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseSupportedViewsChangedEvent(DependencyPropertyChangedEventArgs e)
        {
            bool newSingle, oldSingle, newFacing, oldFacing, newScroll, oldScroll;
            if (e.Property == FlowDocumentReader.IsPageViewEnabledProperty)
            {
                newSingle = (bool)e.NewValue;
                oldSingle = (bool)e.OldValue;
                newFacing = oldFacing = FlowDocumentReader.IsTwoPageViewEnabled;
                newScroll = oldScroll = FlowDocumentReader.IsScrollViewEnabled;
            }
            else if (e.Property == FlowDocumentReader.IsTwoPageViewEnabledProperty)
            {
                newSingle = oldSingle = FlowDocumentReader.IsPageViewEnabled;
                newFacing = (bool)e.NewValue;
                oldFacing = (bool)e.OldValue;
                newScroll = oldScroll = FlowDocumentReader.IsScrollViewEnabled;
            }
            else// if (e.Property == FlowDocumentReader.IsScrollViewEnabledProperty)
            {
                newSingle = oldSingle = FlowDocumentReader.IsPageViewEnabled;
                newFacing = oldFacing = FlowDocumentReader.IsTwoPageViewEnabled;
                newScroll = (bool)e.NewValue;
                oldScroll = (bool)e.OldValue;
            }
            if (newSingle != oldSingle || newFacing != oldFacing || newScroll != oldScroll)
            {
                int[] newViews = GetSupportedViews(newSingle, newFacing, newScroll);
                int[] oldViews = GetSupportedViews(oldSingle, oldFacing, oldScroll);
                RaisePropertyChangedEvent(MultipleViewPatternIdentifiers.SupportedViewsProperty, newViews, oldViews);
            }
        }

        //-------------------------------------------------------------------
        //
        //  Private Members
        //
        //-------------------------------------------------------------------

        #region Private Members

        private int[] GetSupportedViews(bool single, bool facing, bool scroll)
        {
            int count = 0;
            if (single) { count++; }
            if (facing) { count++; }
            if (scroll) { count++; }
            int[] views = count > 0 ? new int[count] : null;
            count = 0;
            if (single) { views[count++] = ConvertModeToViewId(FlowDocumentReaderViewingMode.Page); }
            if (facing) { views[count++] = ConvertModeToViewId(FlowDocumentReaderViewingMode.TwoPage); }
            if (scroll) { views[count++] = ConvertModeToViewId(FlowDocumentReaderViewingMode.Scroll); }
            return views;
        }

        /// <summary>
        /// Converts viewing mode to view id.
        /// </summary>
        private int ConvertModeToViewId(FlowDocumentReaderViewingMode mode)
        {
            return (int)mode;
        }

        /// <summary>
        /// Converts view id to viewing mode.
        /// </summary>
        private FlowDocumentReaderViewingMode ConvertViewIdToMode(int viewId)
        {
            Invariant.Assert(viewId >= 0 && viewId <= 2);
            return (FlowDocumentReaderViewingMode)viewId;
        }

        /// <summary>
        /// FlowDocumentReader associated with the peer.
        /// </summary>
        private FlowDocumentReader FlowDocumentReader
        {
            get { return (FlowDocumentReader)Owner; }
        }

        private DocumentAutomationPeer _documentPeer;

        #endregion Private Members

        //-------------------------------------------------------------------
        //
        //  IMultipleViewProvider Members
        //
        //-------------------------------------------------------------------

        #region IMultipleViewProvider Members

        /// <summary>
        /// <see cref="IMultipleViewProvider.GetViewName"/>
        /// </summary>
        string IMultipleViewProvider.GetViewName(int viewId)
        {
            string name = string.Empty;
            if (viewId >= 0 && viewId <= 2)
            {
                FlowDocumentReaderViewingMode mode = ConvertViewIdToMode(viewId);
                if (mode == FlowDocumentReaderViewingMode.Page)
                {
                    name = SR.Get(SRID.FlowDocumentReader_MultipleViewProvider_PageViewName);
                }
                else if (mode == FlowDocumentReaderViewingMode.TwoPage)
                {
                    name = SR.Get(SRID.FlowDocumentReader_MultipleViewProvider_TwoPageViewName);
                }
                else if (mode == FlowDocumentReaderViewingMode.Scroll)
                {
                    name = SR.Get(SRID.FlowDocumentReader_MultipleViewProvider_ScrollViewName);
                }
            }
            return name;
        }

        /// <summary>
        /// <see cref="IMultipleViewProvider.SetCurrentView"/>
        /// </summary>
        void IMultipleViewProvider.SetCurrentView(int viewId)
        {
            if (viewId >= 0 && viewId <= 2)
            {
                FlowDocumentReader.ViewingMode = ConvertViewIdToMode(viewId);
            }
        }

        /// <summary>
        /// <see cref="IMultipleViewProvider.CurrentView"/>
        /// </summary>
        int IMultipleViewProvider.CurrentView
        {
            get {  return ConvertModeToViewId(FlowDocumentReader.ViewingMode); }
        }

        /// <summary>
        /// <see cref="IMultipleViewProvider.GetSupportedViews"/>
        /// </summary>
        int[] IMultipleViewProvider.GetSupportedViews()
        {
            return GetSupportedViews(
                FlowDocumentReader.IsPageViewEnabled, 
                FlowDocumentReader.IsTwoPageViewEnabled, 
                FlowDocumentReader.IsScrollViewEnabled);
        }

        #endregion IMultipleViewProvider Members
    }
}
