// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Serialization.CustomElements
{
    /// <summary>
    /// CustomCanvas will automatically close its window after it is displayed.
    /// Also, if the Verifier property is set to a Type+function name, it
    /// will be called to run custom verification before the window is closed.
    /// </summary>
    public class CustomCanvas : Canvas, ICustomElement
    {
        /// <summary>
        /// Type+Function to invoke for optional verification.
        /// </summary>
        public static readonly DependencyProperty VerifierProperty = DependencyProperty.RegisterAttached("Verifier", typeof(string), typeof(CustomCanvas), new PropertyMetadata(""));

        /// <summary>
        /// Namespace and class to invoke for optional verification.
        /// </summary>
        public string Verifier
        {
            get { return (string)this.GetValue(VerifierProperty); }
            set { this.SetValue(VerifierProperty, value); }
        }

        /// <summary>
        /// Event callback for render event.
        /// </summary>
        /// <param name="dc"></param>
        protected override void OnRender(DrawingContext dc)
        {
            // Post either the verifier routine or the routine to
            // close the current window.
            CustomElementHelper.PostItem(!String.IsNullOrEmpty(this.Verifier), this);

            base.OnRender(dc);

            // Call event handlers.
            _FireRenderedEvent(this);
        }

        /// <summary>
        /// Event where tells you that the control is already rendered.
        /// </summary>
        public event EventHandler RenderedEvent;

        /// <summary>
        /// Calls handlers of RenderedEvent.
        /// </summary>
        private void _FireRenderedEvent(object element)
        {
            // Call event handlers.
            if (RenderedEvent != null)
            {
                GlobalLog.LogDebug("Firing Rendered event...");
                RenderedEvent(element, EventArgs.Empty);
            }
        }
    }
}
