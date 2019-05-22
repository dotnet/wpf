// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: The ContentHostHelper class contains static methods that are
//              useful for performing common tasks with IContentHost.
//

using System;                               // Object
using System.Collections.Generic;           // List<T>
using System.Windows;                       // IContentHost
using System.Windows.Controls;              // TextBlock
using System.Windows.Controls.Primitives;   // DocumentPageView
using System.Windows.Documents;             // FlowDocument
using System.Windows.Media;                 // Visual
using MS.Internal.PtsHost;                  // FlowDocumentPage

namespace MS.Internal.Documents
{
    /// <summary>
    /// Static helper functions for dealing with IContentHost.
    /// </summary>
    internal static class ContentHostHelper
    {
        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Given a ContentElement searches for associated IContentHost, if one exists.
        /// </summary>
        /// <param name="contentElement">Content element</param>
        /// <returns>Associated IContentHost with ContentElement.</returns>
        internal static IContentHost FindContentHost(ContentElement contentElement)
        {
            IContentHost ich = null;
            DependencyObject parent;
            TextContainer textContainer;

            if (contentElement == null) { return null; }

            // If the ContentElement is a TextElement, retrieve IContentHost form the owner
            // of TextContainer.
            if (contentElement is TextElement)
            {
                textContainer = ((TextElement)contentElement).TextContainer;
                parent = textContainer.Parent;
                if (parent is IContentHost) // TextBlock
                {
                    ich = (IContentHost)parent;
                }
                else if (parent is FlowDocument) // Viewers
                {
                    ich = GetICHFromFlowDocument((TextElement)contentElement, (FlowDocument)parent);
                }
                else if (textContainer.TextView != null && textContainer.TextView.RenderScope is IContentHost)
                {
                    // TextBlock hosted in ControlTemplate
                    ich = (IContentHost)textContainer.TextView.RenderScope;
                }
            }
            // else; cannot retrive IContentHost

            return ich;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Given a ContentElement within FlowDocument searches for associated IContentHost.
        /// </summary>
        /// <param name="contentElement">Content element</param>
        /// <param name="flowDocument">FlowDocument hosting ContentElement.</param>
        /// <returns>Associated IContentHost with ContentElement.</returns>
        private static IContentHost GetICHFromFlowDocument(TextElement contentElement, FlowDocument flowDocument)
        {
            IContentHost ich = null;
            List<DocumentPageView> pageViews;
            ITextView textView = flowDocument.StructuralCache.TextContainer.TextView;

            if (textView != null)
            {
                // If FlowDocument is hosted by FlowDocumentScrollViewer, the RenderScope
                // is FlowDocumentView object which hosts PageVisual representing the content.
                // This PageVisual is also IContentHost for the entire content of DocumentPage.
                if (textView.RenderScope is FlowDocumentView) // FlowDocumentScrollViewer
                {
                    if (VisualTreeHelper.GetChildrenCount(textView.RenderScope) > 0)
                    {
                        ich = VisualTreeHelper.GetChild(textView.RenderScope, 0) as IContentHost;
                    }
                }
                // Our best guess is that FlowDocument is hosted by DocumentViewerBase.
                // In this case search the style for all DocumentPageViews.
                // Having collection of DocumentPageViews, find for the one which hosts TextElement.
                else if (textView.RenderScope is FrameworkElement)
                {
                    pageViews = new List<DocumentPageView>();
                    FindDocumentPageViews(textView.RenderScope, pageViews);
                    for (int i = 0; i < pageViews.Count; i++)
                    {
                        if (pageViews[i].DocumentPage is FlowDocumentPage)
                        {
                            textView = (ITextView)((IServiceProvider)pageViews[i].DocumentPage).GetService(typeof(ITextView));
                            if (textView != null && textView.IsValid)
                            {
                                // Check if the page contains ContentElement. Check Start and End
                                // position, which will give desired results in most of the cases.
                                // Having hyperlink spanning more than 2 pages is not very common,
                                // and this code will not work with it correctly.
                                if (textView.Contains(contentElement.ContentStart) ||
                                    textView.Contains(contentElement.ContentEnd))
                                {
                                    ich = pageViews[i].DocumentPage.Visual as IContentHost;
                                }
                            }
                        }
                    }
                }
            }

            return ich;
        }

        /// <summary>
        /// Does deep Visual tree walk to retrieve all DocumentPageViews.
        /// It stops recursing down into visual tree in following situations:
        /// a) Visual is UIElement and it is not part of Contol Template,
        /// b) Visual is DocumentPageView.
        /// </summary>
        /// <param name="root">FrameworkElement that is part of Control Template.</param>
        /// <param name="pageViews">Collection of DocumentPageViews; found elements are appended here.</param>
        /// <returns>Whether collection of DocumentPageViews has been updated.</returns>
        private static void FindDocumentPageViews(Visual root, List<DocumentPageView> pageViews)
        {
            Invariant.Assert(root != null);
            Invariant.Assert(pageViews != null);

            if (root is DocumentPageView)
            {
                pageViews.Add((DocumentPageView)root);
            }
            else
            {
                FrameworkElement fe;
                // Do deep tree walk to retrieve all DocumentPageViews.
                // It stops recursing down into visual tree in following situations:
                // a) Visual is UIElement and it is not part of Contol Template,
                // b) Visual is DocumentPageView.
                // Add to collection any DocumentPageViews found in the Control Template.
                int count = root.InternalVisualChildrenCount;
                for (int i = 0; i < count; i++)
                {
                    Visual child = root.InternalGetVisualChild(i);
                    fe = child as FrameworkElement;
                    if (fe != null)
                    {
                        if (fe.TemplatedParent != null)
                        {
                            if (fe is DocumentPageView)
                            {
                                pageViews.Add(fe as DocumentPageView);
                            }
                            else
                            {
                                FindDocumentPageViews(fe, pageViews);
                            }
                        }
                    }
                    else
                    {
                        FindDocumentPageViews(child, pageViews);
                    }
                }
            }
        }

        #endregion Private Methods
    }
}
