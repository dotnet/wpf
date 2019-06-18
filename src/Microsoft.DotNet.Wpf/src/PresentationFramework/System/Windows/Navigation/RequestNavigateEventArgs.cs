// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 

using System;
using System.Net;
using System.Windows;

using MS.Internal.Utility;

namespace System.Windows.Navigation 
{
    /// <summary>
    /// EventArgs for RequestNavigate
    /// </summary>
    /// <ExternalAPI/> 
    public class RequestNavigateEventArgs : RoutedEventArgs
    {
        Uri _uri;
        string _target;

        /// <summary> 
        /// Default constructor
        /// </summary>
        /// <ExternalAPI/> 
        protected RequestNavigateEventArgs() : base()
        {
            base.RoutedEvent=System.Windows.Documents.Hyperlink.RequestNavigateEvent;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uri">Uri to navigate</param>
        /// <param name="target">Name of the target navigator</param>
        /// <ExternalAPI/> 
        public RequestNavigateEventArgs(Uri uri, string target) : base()
        {
            _uri = uri;
            _target = target;

            base.RoutedEvent=System.Windows.Documents.Hyperlink.RequestNavigateEvent;
        }

        /// <summary>
        /// Uri to navigate
        /// </summary>
        /// <ExternalAPI Inherit="true"/>
        public Uri Uri
        {
            get{return _uri;}
        }

        /// <summary>
        /// Target window or frame to perform navigation
        /// </summary>
        /// <ExternalAPI Inherit="true"/>
        public string Target
        {
            get{return _target;}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="genericHandler"></param>
        /// <param name="genericTarget"></param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (RoutedEvent == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.RequestNavigateEventMustHaveRoutedEvent));
            }

            RequestNavigateEventHandler handler = (RequestNavigateEventHandler)genericHandler;

            handler(genericTarget, this);
        }            
    }

    /// <summary>
    /// Delegate that handles RequestNavigate event.
    /// </summary>
    /// <ExternalAPI Inherit="true"/>
    public delegate void RequestNavigateEventHandler(object sender, RequestNavigateEventArgs e);
}
