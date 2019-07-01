// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create WebBrowser
    /// </summary>
    [TargetTypeAttribute(typeof(WebBrowser))]
    internal class WebBrowserFactory : DiscoverableFactory<WebBrowser>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an object to set WebBrowser ObjectForScripting property.
        /// </summary>
        public object ObjectForScripting { get; set; }

        /// <summary>
        /// Gets or sets a Uri to set WebBrowser Source property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Uri Source { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a WebBrowser.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override WebBrowser Create(DeterministicRandom random)
        {
            WebBrowser webBrower = new WebBrowser();

            //ObjectForScripting type need be visible to COM.
            if (ObjectForScripting != null && Marshal.IsTypeVisibleFromCom(ObjectForScripting.GetType()))
            {
                webBrower.ObjectForScripting = ObjectForScripting;
            }

            webBrower.Source = Source;
            webBrower.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(LoadCompleted);
            webBrower.Navigated += new System.Windows.Navigation.NavigatedEventHandler(Navigated);
            webBrower.Navigating += new System.Windows.Navigation.NavigatingCancelEventHandler(Navigating);

            return webBrower;
        }

        #endregion

        private void Navigating(object sender, NavigatingCancelEventArgs e)
        {
            Trace.WriteLine("WebBrowser navigating.");
        }

        private void Navigated(object sender, NavigationEventArgs e)
        {
            Trace.WriteLine("WebBrowser navigated.");
        }

        private void LoadCompleted(object sender, NavigationEventArgs e)
        {
            Trace.WriteLine("WebBrowser load completed.");
        }
    }
}
