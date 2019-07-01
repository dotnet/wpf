// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Runtime.InteropServices;
using System.IO;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// LostDevice action create a full-screen directx application, and shut it down. 
    /// Dependency: this action need NativeD3DApp.dll, binplaced to folder Common\Imaging\D3DImage
    /// </summary>
    public class LostDeviceAction : SimpleDiscoverableAction
    {
        public double OccurrenceIndicator { get; set; }

        #region IAction Members
        public override bool CanPerform()
        {
            if (!File.Exists("NativeD3DApp.dll"))
            {
                throw new TestSetupException("File : NativeD3DApp.dll, on which LostDeviceAction depends, not found.");
            }

            //Do action only when OccurrenceIndicator is less than 0.002(1/500). 
            return OccurrenceIndicator < 0.002;
        }

        public override void Perform()
        {
            // Full screen method are not thread safe. 
            lock (fullScreenApplication)
            {
                
                // Initialize a full-screen application
                if (!InitializeFullScreen())
                {
                    System.Diagnostics.Trace.WriteLine("Failed to initialize a FullScreen application.");
                }
                else
                {
                    // shutdown the full-screen application
                    ShutdownFullScreen();
                }
            }
        }

        /// <summary>
        /// Initializes a full screen application
        /// </summary>
        /// <returns></returns>
        [DllImport("NativeD3DApp.dll")]
        public static extern bool InitializeFullScreen();

        /// <summary>
        /// Shuts down a previously created full screen application
        /// </summary>
        [DllImport("NativeD3DApp.dll")]
        public static extern void ShutdownFullScreen();

        private static readonly Object fullScreenApplication = new Object();

        #endregion
    }
}
