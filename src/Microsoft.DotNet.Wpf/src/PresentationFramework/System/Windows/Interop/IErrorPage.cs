// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Threading;

namespace System.Windows.Interop
{
    /// <summary>
    /// Interface defines interaction between Xapplauncher and host supplied error page. 
    /// </summary>
    public interface IErrorPage
    {
        /// <summary>
        /// Path to Deployment Uri
        /// </summary>
        Uri DeploymentPath { get; set;}

        /// <summary>
        /// Title for error message
        /// </summary>
        string ErrorTitle { get; set;}

        /// <summary>
        /// Text for error message
        /// </summary>
        string ErrorText { get; set;}

        /// <summary>
        /// Bool: True=>Error, False-> non-error (Cancel)
        /// </summary>
        bool ErrorFlag { get; set;}

        /// <summary>
        /// Path to log file for Clickonce
        /// </summary>
        string LogFilePath { get; set;}

        /// <summary>
        /// Support uri for application
        /// </summary>
        Uri SupportUri { get; set;}

        /// <summary>
        /// Callback when user hits refresh
        /// </summary>
        DispatcherOperationCallback RefreshCallback { get; set;}

        /// <summary>
        /// Callback when user clicks GetWinFx button
        /// </summary>
        DispatcherOperationCallback GetWinFxCallback { get; set;}
    }
}