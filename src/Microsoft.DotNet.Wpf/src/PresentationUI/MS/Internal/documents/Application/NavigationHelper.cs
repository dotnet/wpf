// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//  NavigationHelper is an internal utility class for Mongoose to deal
//  with Uri navigations.
using System;
using System.Security;
using System.Windows.Interop;
using System.Windows.TrustUI;
using MS.Internal;
using MS.Internal.PresentationUI;

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// Helper class for handling browser navigations.
    /// </summary>
    internal static class NavigationHelper
    {
        ///// <summary>
        ///// Invokes a navigation to a new document
        ///// </summary>
        internal static void NavigateToDocument(Document document)
        {
            Trace.SafeWrite(
               Trace.File,
               "Attempting to navigate to new document {0}.",
               document.Uri);

            Invariant.Assert(
               _navigate != null,
               "Navigation object has not been instantiated.");

            Invariant.Assert(
                _navigate.Value != null,
                "Navigation delegate has not been assigned.");

            Invariant.Assert(
                document != null,
                "Target document has not been assigned.");

            _navigate.Value(new SecurityCriticalData<Uri>(document.Uri));
        }

        ///// <summary>
        ///// Invokes a top-level browserNavigation action to the specified Uri.
        ///// </summary>
        internal static void NavigateToExternalUri(Uri uri)
        {
            Trace.SafeWrite(
                Trace.File,
                "Attempting to navigate to external Uri {0}.",
                uri);

            Invariant.Assert(
                _navigate != null,
                "Navigation object has not been instantiated.");

            Invariant.Assert(
                _navigate.Value != null,
                "Navigation delegate has not been assigned.");

            Invariant.Assert(
                uri != null,
                "Target uri has not been assigned.");

            _navigate.Value(new SecurityCriticalData<Uri>(uri));
        }


        /// <summary>
        /// A delegate that will navigate the root browser window.
        /// </summary>
        /// <remarks>
        /// If we are going to add more functionality a IBrowserService interface
        /// of some type should be defined and set vs many delegates.
        /// </remarks>
        [FriendAccessAllowed]
        internal static NavigateDelegate Navigate
        {
            get
            {
                if (_navigate != null)
                {
                    return _navigate.Value;
                }
                return null;
            }

            set
            {
                _navigate = new SecurityCriticalDataClass<NavigateDelegate>(value);
            }
        }

        internal delegate void NavigateDelegate(SecurityCriticalData<Uri> uri);

        private static SecurityCriticalDataClass<NavigateDelegate> _navigate;
    }
}
