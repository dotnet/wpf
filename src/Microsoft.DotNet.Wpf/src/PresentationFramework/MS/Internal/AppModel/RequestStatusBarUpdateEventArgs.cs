// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Windows;
using MS.Internal.Utility;
using System.Security;

namespace MS.Internal.AppModel
{
    internal sealed class RequestSetStatusBarEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Text that will be set on the status bar.
        /// </summary>
        private SecurityCriticalDataForSet<string> _text;

        /// <summary>
        /// Creates a RequestSetStatusBarEventArgs based on a specified string.
        /// </summary>
        /// <param name="text">Text that will be set on the status bar.</param>
        internal RequestSetStatusBarEventArgs(string text)
            : base()
        {
            _text.Value = text;
            base.RoutedEvent = System.Windows.Documents.Hyperlink.RequestSetStatusBarEvent;
        }

        /// <summary>
        /// Creates a RequestSetStatusBarEventArgs based on a specified URI.
        /// </summary>
        /// <param name="targetUri">URI that will be set on the status bar after appropriate conversion to text. If null, the status bar will be cleared.</param>
        internal RequestSetStatusBarEventArgs(Uri targetUri)
            : base()
        {
            if (targetUri == null)
                _text.Value = String.Empty;
            else
                _text.Value = BindUriHelper.UriToString(targetUri);

            base.RoutedEvent = System.Windows.Documents.Hyperlink.RequestSetStatusBarEvent;
        }

        /// <summary>
        /// Text that will be set on the status bar.
        /// </summary>
        internal string Text
        {
            get
            {
                return _text.Value;
            }
        }

        /// <summary>
        /// Request object for clearing the status bar.
        /// </summary>
        internal static RequestSetStatusBarEventArgs Clear
        {
            get
            {
                return new RequestSetStatusBarEventArgs(String.Empty);
            }
        }
    }
}

