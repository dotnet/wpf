// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;
using System.Globalization;
using System.Diagnostics;
using MS.Internal;

namespace MS.Internal.AppModel
{
    internal static class NavigationHelper
    {
        /// <summary>
        /// See INavigatorImpl.FindRootViewer().
        /// </summary>
        internal static Visual FindRootViewer(ContentControl navigator, string contentPresenterName)
        {
            object content = navigator.Content;
            if (content == null || content is Visual)
                return content as Visual;

            ContentPresenter cp = null;
            if (navigator.Template != null)
            {
                cp = (ContentPresenter)navigator.Template.FindName(contentPresenterName, navigator);
            }

            // If null, either <contentPresenterName> is not defined in the current template or the template 
            // has not been applied yet. 
            if (cp == null || cp.InternalVisualChildrenCount == 0/*Layout not done yet*/)
                return null;
            Visual v = cp.InternalGetVisualChild(0);
            return v;
        }
    };
}
