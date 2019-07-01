// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

// Abstract base class for UIHandlers
namespace Microsoft.Test.Loaders 
{

    /// <summary>
    /// Action preformed by a UIHandler
    /// </summary>
    
    public enum UIHandlerAction 
    {
        /// <summary>
        /// The UI was handled, stop processing any other handlers registered for this UI
        /// </summary>
        Handled,
        /// <summary>
        /// The UI was not handled, continue processing other handlers registered for this UI
        /// </summary>
        Unhandled,
        /// <summary>
        /// Send a UIHandler Abort signal
        /// </summary>
        Abort
    }   

    /// <summary>
    /// Base class for creating ApplicationMonitor UIHandlers
    /// </summary>
    
    public abstract class UIHandler
    {

        #region Private Members 

        string processName = null;
        string windowTitle = null;
        string namedRegistration = null;
        UIHandlerNotification notification = UIHandlerNotification.All;
        LoaderStep step = null;
        bool allowMultipleInvocations = false;
        WindowClassEnum windowClass = WindowClassEnum.Any;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new UIHandler
        /// </summary>
        protected UIHandler() 
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// This method is called to allow the UIHandler to handle a specific window
        /// </summary>
        /// <param name="topLevelhWnd">the top level window hosting the UI</param>
        /// <param name="hwnd">hwnd of the targeted UI</param>
        /// <param name="process">Process that is hosting the window</param>
        /// <param name="title">title of the window</param>
        /// <param name="notification">Indication of why the UIhandler is being notified about the UI</param>
        /// <returns>the Action preformed by the UIHandler on the Window</returns>
        public abstract UIHandlerAction HandleWindow(IntPtr topLevelhWnd, IntPtr hwnd, Process process, string title, UIHandlerNotification notification);

        /// <summary>
        /// gets or sets the name of the process to register this UIHandler for using ActivationStep
        /// </summary>
        public string ProcessName 
        {
            get { return processName; }
            set { processName = value; }
        }

        /// <summary>
        /// gets or sets the WindowTitle to register this UIHandler for using ActivationStep
        /// </summary>
        public string WindowTitle 
        {
            get { return windowTitle; }
            set { windowTitle = value; }
        }

        /// <summary>
        /// gets or sets the Named Registration to register this UIHandler for using ActivationStep.
        /// </summary>
        public string NamedRegistration 
        {
            get { return namedRegistration; }
            set { namedRegistration = value; }
        }

        /// <summary>
        /// gets or sets the Notification to register this UIHandler for using ActivationStep.
        /// </summary>
        public UIHandlerNotification Notification 
        {
            get { return notification; }
            set { notification = value; }
        }

        /// <summary>
        /// gets or sets the WindowClass to look for
        /// </summary> 
	    public WindowClassEnum WindowClass 
        {
            get { return windowClass; }
            set { windowClass = value; }
        }

        /// <summary>
        /// gets or sets a value indicating whether the UIHandler can be invoked multiple times by the same registration.
        /// </summary>
        public bool AllowMultipleInvocations {
            get { return allowMultipleInvocations; }
            set { allowMultipleInvocations = value; }
        }
              
        /// <summary>
        /// gets or sets the related LoaderStep for this handler
        /// </summary>
        /// <value>the LoaderStep if the accociation exsists, otherwise, null</value>
        /// <remarks>
        /// LoaderSteps that have a notion of UIHandlers should set this property on those
        /// handlers to themselves so that the handler may discover the context in which
        /// it is running.
        /// </remarks>
        public LoaderStep Step 
        {
            get { return step; }
            set { step = value; }
        }

        #endregion

    }
    
}
