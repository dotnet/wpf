// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the help class of FixedHyperLink. 
//

namespace System.Windows.Documents
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows.Threading;
    using System.Windows.Markup;
    using System.Windows.Navigation;
    using System.Windows.Media;

    ///<summary>
    ///     The IFixedNavigate interface will be implemented by FixedPage, FixedDocument, 
    ///     and FixedDocumentSequence to support fixed hyperlink.
    ///</summary>
    internal interface IFixedNavigate
    {
        /// <summary>
        /// Find the element which given ID in this document context.
        /// </summary>
        /// <param name="elementID">The ID of UIElement to search for</param>
        /// <param name="rootFixedPage">The fixedPage that contains returns UIElement</param>
        /// <returns></returns>
        UIElement FindElementByID(string elementID, out FixedPage rootFixedPage);

        /// <summary>
        /// Navigate to the element with ID= elementID
        /// </summary>
        /// <param name="elementID"></param>
        void NavigateAsync (string  elementID);
    }

    internal static class FixedHyperLink
    {
        /// <summary>
        ///     NavigationService property ChangedCallback.
        /// </summary>
        public static void OnNavigationServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FixedDocument fixedContent = d as FixedDocument;

            if (fixedContent != null)
            {
                NavigationService oldService = (NavigationService) e.OldValue;
                NavigationService newService = (NavigationService) e.NewValue;

                if (oldService != null)
                {
                    oldService.FragmentNavigation -= new FragmentNavigationEventHandler(FragmentHandler);
                }

                if (newService != null)
                {
                    newService.FragmentNavigation += new FragmentNavigationEventHandler(FragmentHandler);
                }
            }
        }

        /// <summary>
        /// Called by NavigationService to let document content to handle the fragment first.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void FragmentHandler(object sender, FragmentNavigationEventArgs e)
        {
            NavigationService ns = sender as NavigationService;

            if (ns != null)
            {
                string fragment = e.Fragment;
                IFixedNavigate fixedNavigate = ns.Content as IFixedNavigate;

                if (fixedNavigate != null)
                {
                    fixedNavigate.NavigateAsync(e.Fragment);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Fire BringinToView event on the element ID.
        /// </summary>
        /// <param name="ElementHost">The host document of element ID, call any one implents IFixedNavigate</param>
        /// <param name="elementID"></param>
        internal static void NavigateToElement(object ElementHost, string elementID)
        {
            FixedPage rootFixedPage = null;
            FrameworkElement targetElement = null;

            targetElement = ((IFixedNavigate)ElementHost).FindElementByID(elementID, out rootFixedPage) as FrameworkElement;

            if (targetElement != null)
            {
                if (targetElement is FixedPage)
                {
                    //
                    // For fixedpage, we only need to scroll to page position.
                    //
                    targetElement.BringIntoView();
                }
                else 
                {
                    //Just passing in raw rect of targetElement.  Let DocumentViewer/Grid handle transforms
                    targetElement.BringIntoView(targetElement.VisualContentBounds);
                }
            }
            return;
        }
    }
}