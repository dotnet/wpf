// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Represents the event args passed to AutomatedApplication focus changed events.
    /// </summary>
    public class AutomatedApplicationFocusChangedEventArgs : AutomatedApplicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the AutomatedApplicationFocusChangedEventArgs
        /// class.
        /// </summary>
        /// <param name="automatedApp">
        /// The AutomatedApplication data to pass to the listeners.
        /// </param>
        /// <param name="newFocusedElement">
        /// The new focused element data to pass the listeners. This can be an AutomationElement 
        /// for an out-of-process scenario or a UIElement for an in-process WPF scenario.
        /// </param>
        public AutomatedApplicationFocusChangedEventArgs(AutomatedApplication automatedApp, object newFocusedElement)
            : base(automatedApp)
        {
            NewFocusedElement = newFocusedElement;
        }

        /// <summary>
        /// The new focused element passed to the listeners.
        /// </summary>
        public object NewFocusedElement
        {
            get;
            set;
        }
    }
}
