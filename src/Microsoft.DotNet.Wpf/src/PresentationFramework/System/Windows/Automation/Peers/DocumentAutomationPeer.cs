// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: AutomationPeer associated with flow and fixed documents.
//

using System.Collections.Generic;           // List<T>
using System.Security;                      // SecurityCritical
using System.Windows.Documents;             // ITextContainer
using System.Windows.Interop;               // HwndSource
using System.Windows.Media;                 // Visual
using MS.Internal;                          // PointUtil
using MS.Internal.Automation;               // TextAdaptor
using MS.Internal.Documents;                // TextContainerHelper

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer associated with flow and fixed documents.
    /// </summary>
    public class DocumentAutomationPeer : ContentTextAutomationPeer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public DocumentAutomationPeer(FrameworkContentElement owner)
            : base(owner)
        {
            if (owner is IServiceProvider)
            {
                _textContainer = ((IServiceProvider)owner).GetService(typeof(ITextContainer)) as ITextContainer;
                if (_textContainer != null)
                {
                    _textPattern = new TextAdaptor(this, _textContainer);
                }
            }
        }

        /// <summary>
        /// Notify the peer that it has been disconnected.
        /// </summary>
        internal void OnDisconnected()
        {
            if (_textPattern != null)
            {
                _textPattern.Dispose();
                _textPattern = null;
            }
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetChildrenCore"/>
        /// </summary>
        /// <remarks>
        /// Since DocumentAutomationPeer gives access to its content through 
        /// TextPattern, this method always returns null.
        /// </remarks>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            if (_childrenStart != null && _childrenEnd != null)
            {
                ITextContainer textContainer = ((IServiceProvider)Owner).GetService(typeof(ITextContainer)) as ITextContainer;
                return TextContainerHelper.GetAutomationPeersFromRange(_childrenStart, _childrenEnd, textContainer.Start);
            }
            return null;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationControlTypeCore"/>
        /// </summary>
        public override object GetPattern(PatternInterface patternInterface)
        {
            object returnValue = null;

            if (patternInterface == PatternInterface.Text)
            {
                if (_textPattern == null)
                {
                    if (Owner is IServiceProvider)
                    {
                        ITextContainer textContainer = ((IServiceProvider)Owner).GetService(typeof(ITextContainer)) as ITextContainer;
                        if (textContainer != null)
                        {
                            _textPattern = new TextAdaptor(this, textContainer);
                        }
                    }
                }
                returnValue = _textPattern;
            }
            else
            {
                returnValue = base.GetPattern(patternInterface);
            }
            return returnValue;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationControlTypeCore"/>
        /// </summary>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Document;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClassNameCore"/>
        /// </summary>
        /// <returns></returns>
        protected override string GetClassNameCore()
        {
            return "Document";
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsControlElementCore"/>
        /// </summary>
        protected override bool IsControlElementCore()
        {
            // We only want this peer to show up in the Control view if it is visible
            // For compat we allow falling back to legacy behavior (returning true always)
            // based on AppContext flags, IncludeInvisibleElementsInControlView evaluates them.
            if (IncludeInvisibleElementsInControlView)
            {
                return true;
            }

            ITextView textView = _textContainer?.TextView;
            UIElement uiElement = textView?.RenderScope;
            return uiElement?.IsVisible == true;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetBoundingRectangleCore"/>
        /// </summary>
        protected override Rect GetBoundingRectangleCore()
        {
            UIElement uiScope;
            Rect boundingRect = CalculateBoundingRect(false, out uiScope);
            if (boundingRect != Rect.Empty && uiScope != null)
            {
                HwndSource hwndSource = PresentationSource.CriticalFromVisual(uiScope) as HwndSource;
                if (hwndSource != null)
                {
                    boundingRect = PointUtil.ElementToRoot(boundingRect, uiScope, hwndSource);
                    boundingRect = PointUtil.RootToClient(boundingRect, hwndSource);
                    boundingRect = PointUtil.ClientToScreen(boundingRect, hwndSource);
                }
            }
            return boundingRect;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClickablePointCore"/>
        /// </summary>
        protected override Point GetClickablePointCore()
        {
            Point point = new Point();
            UIElement uiScope;
            Rect boundingRect = CalculateBoundingRect(true, out uiScope);
            if (boundingRect != Rect.Empty && uiScope != null)
            {
                HwndSource hwndSource = PresentationSource.CriticalFromVisual(uiScope) as HwndSource;
                if (hwndSource != null)
                {
                    boundingRect = PointUtil.ElementToRoot(boundingRect, uiScope, hwndSource);
                    boundingRect = PointUtil.RootToClient(boundingRect, hwndSource);
                    boundingRect = PointUtil.ClientToScreen(boundingRect, hwndSource);
                    point = new Point(boundingRect.Left + boundingRect.Width * 0.1, boundingRect.Top + boundingRect.Height * 0.1);
                }
            }
            return point;
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsOffscreenCore"/>
        /// </summary>
        protected override bool IsOffscreenCore()
        {
            IsOffscreenBehavior behavior = AutomationProperties.GetIsOffscreenBehavior(Owner);

            switch (behavior)
            {
                case IsOffscreenBehavior.Onscreen :
                    return false;

                case IsOffscreenBehavior.Offscreen :
                    return true;

                default:
                    {
                        UIElement uiScope;
                        Rect boundingRect = CalculateBoundingRect(true, out uiScope);
                        return (DoubleUtil.AreClose(boundingRect, Rect.Empty) || uiScope == null);
                    }
            }
        }

        /// <summary>
        /// Gets collection of AutomationPeers for given text range.
        /// </summary>
        internal override List<AutomationPeer> GetAutomationPeersFromRange(ITextPointer start, ITextPointer end)
        {
            _childrenStart = start.CreatePointer();
            _childrenEnd = end.CreatePointer();
            ResetChildrenCache();
            return GetChildren();
        }

        /// <summary>
        /// Calculate visible rectangle.
        /// </summary>
        private Rect CalculateBoundingRect(bool clipToVisible, out UIElement uiScope)
        {
            uiScope = null;
            Rect boundingRect = Rect.Empty;
            if (Owner is IServiceProvider)
            {
                ITextContainer textContainer = ((IServiceProvider)Owner).GetService(typeof(ITextContainer)) as ITextContainer;
                ITextView textView = (textContainer != null) ? textContainer.TextView : null;
                if (textView != null)
                {
                    // Make sure TextView is updated
                    if (!textView.IsValid)
                    {
                        if (!textView.Validate())
                        {
                            textView = null;
                        }
                        if (textView != null && !textView.IsValid)
                        {
                            textView = null;
                        }
                    }
                    // Get bounding rect from TextView.
                    if (textView != null)
                    {
                        boundingRect = new Rect(textView.RenderScope.RenderSize);
                        uiScope = textView.RenderScope;

                        // Compute visible portion of the rectangle.
                        if (clipToVisible)
                        {
                            Visual visual = textView.RenderScope;
                            while (visual != null && boundingRect != Rect.Empty)
                            {
                                if (VisualTreeHelper.GetClip(visual) != null)
                                {
                                    GeneralTransform transform = textView.RenderScope.TransformToAncestor(visual).Inverse;
                                    // Safer version of transform to descendent (doing the inverse ourself), 
                                    // we want the rect inside of our space. (Which is always rectangular and much nicer to work with)
                                    if (transform != null)
                                    {
                                        Rect clipBounds = VisualTreeHelper.GetClip(visual).Bounds;
                                        clipBounds = transform.TransformBounds(clipBounds);
                                        boundingRect.Intersect(clipBounds);
                                    }
                                    else
                                    {
                                        // No visibility if non-invertable transform exists.
                                        boundingRect = Rect.Empty;
                                    }
                                }
                                visual = VisualTreeHelper.GetParent(visual) as Visual;
                            }
                        }
                    }
                }
            }
            return boundingRect;
        }

        private ITextPointer _childrenStart;
        private ITextPointer _childrenEnd;
        private TextAdaptor _textPattern;
        private ITextContainer _textContainer;
    }
}
