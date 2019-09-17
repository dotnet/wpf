// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Threading;

namespace System.Windows.Interop
{
    /// <summary>
    /// Interface defines the interaction between xapp launcher and host provided progress page
    /// </summary>
    public interface IProgressPage
    {
        /// <summary>
        /// Path to Deployment Uri
        /// </summary>
        Uri DeploymentPath { get; set;}

        /// <summary>
        /// Callback when user hits stop
        /// </summary>
        DispatcherOperationCallback StopCallback { get; set;}

        /// <summary>
        /// Callback when user hits refresh
        /// </summary>
        DispatcherOperationCallback RefreshCallback { get; set;}

       
        /// <summary>
        /// Name of Application
        /// </summary>
        string ApplicationName { get; set;}

        /// <summary>
        /// Name of Publisher
        /// </summary>
        string PublisherName { get; set;}

        /// <summary>
        /// Updates progress
        /// </summary>
        /// <param name="bytesDownloaded">Total bytes downloaded</param>
        /// <param name="bytesTotal">Total bytes to be downloaded</param>
        void UpdateProgress(long bytesDownloaded, long bytesTotal);
    }
}