// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Defines the contract for an AutomatedApplication.
    /// </summary>
    /// <remarks>
    /// Represents the 'Implemention' inteface for the AutomatedApplication bridge. As such, 
    /// this can vary from the public interface of AutomatedApplication.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Impl")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IAutomatedApplicationImpl
    {
        /// <summary>
        /// Starts the test application.
        /// </summary>
        void Start();

        /// <summary>
        /// Closes the test application.
        /// </summary>
        void Close();

        /// <summary>
        /// Waits for the test application's main window to open.
        /// </summary>
        /// <param name="timeout">The timeout interval.</param>
        void WaitForMainWindow(TimeSpan timeout);

        /// <summary>
        /// Waits for the given window to open.
        /// </summary>
        /// <param name="windowName">The window id of the window to wait for.</param>
        /// <param name="timeout">The timeout interval.</param>
        void WaitForWindow(string windowName, TimeSpan timeout);        

        /// <summary>
        /// Waits for the test application to become idle.
        /// </summary>
        /// <param name="timeout">The timeout interval.</param>
        void WaitForInputIdle(TimeSpan timeout);

        /// <summary>
        /// Occurs when the test application's main window is opened.
        /// </summary>
        event EventHandler MainWindowOpened;

        /// <summary>
        /// Occurs when the test application exits.
        /// </summary>
        event EventHandler Exited;

        /// <summary>
        /// Occurs when focus changes.
        /// </summary>
        event EventHandler FocusChanged;

        /// <summary>
        /// The test application's main window.
        /// </summary>
        object MainWindow { get; }

        /// <summary>
        /// The driver of the test application.
        /// </summary>
        object ApplicationDriver { get; }

        /// <summary>
        /// Indicates whether the test application's main window has opened.
        /// </summary>
        bool IsMainWindowOpened { get; }
    }
}
