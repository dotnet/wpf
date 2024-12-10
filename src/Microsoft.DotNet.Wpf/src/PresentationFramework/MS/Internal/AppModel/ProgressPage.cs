// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//  Description:
//      Deployment progress page. This is primarily a proxy to the native progress page, which supersedes
//      the managed one from up to v3.5. See Host\DLL\ProgressPage.hxx for details.
//

using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Win32.Foundation;

namespace MS.Internal.AppModel
{
    [ComImport, Guid("1f681651-1024-4798-af36-119bbe5e5665")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface INativeProgressPage
    {
        [PreserveSig]
        HRESULT Show();
        [PreserveSig]
        HRESULT Hide();
        [PreserveSig]
        HRESULT ShowProgressMessage(string message);
        [PreserveSig]
        HRESULT SetApplicationName(string appName);
        [PreserveSig]
        HRESULT SetPublisherName(string publisherName);
        [PreserveSig]
        HRESULT OnDownloadProgress(ulong bytesDownloaded, ulong bytesTotal);
    };

    /// <remarks>
    /// IProgressPage is public. It was introduced for the Media Center integration, which is now considered
    /// deprecated, but we have to support it at least for as long as we keep doing in-place upgrades.
    /// </remarks>
    interface IProgressPage2 : IProgressPage
    {
        void ShowProgressMessage(string message);
    };

    class NativeProgressPageProxy : IProgressPage2
    {
        internal NativeProgressPageProxy(INativeProgressPage npp)
        {
            _npp = npp;
        }

        public void ShowProgressMessage(string message)
        {
            // Ignore the error code.  This page is transient and it's not the end of the world if this doesn't show up.
            _ = _npp.ShowProgressMessage(message);
        }

        public Uri DeploymentPath
        {
            set { }
            get { throw new NotImplementedException(); }
        }

        /// <remarks>
        /// The native progress page sends a stop/cancel request to its host object, which then calls 
        /// IBrowserHostServices.ExecCommand(OLECMDID_STOP).
        /// </remarks>
        public DispatcherOperationCallback StopCallback
        {
            set { }
            get { throw new NotImplementedException(); }
        }

        /// <remarks>
        /// The native progress page sends a Refresh request to its host object, which then calls 
        /// IBrowserHostServices.ExecCommand(OLECMDID_REFRESH).
        /// </remarks>
        public DispatcherOperationCallback RefreshCallback
        {
            set { }
            get { return null; }
        }

        public string ApplicationName
        {
            set
            {
                // Ignore the error code.  This page is transient and it's not the end of the world if this doesn't show up.
                _ = _npp.SetApplicationName(value);
            }

            get { throw new NotImplementedException(); }
        }

        /// <SecurityNOoe>
        /// Critical: Calls a SUC'd COM interface method.
        /// TreatAsSafe: 1) The publisher name is coming from the manifest, so it could be anything.
        ///       This means the input doesn't need to be trusted. 
        ///     2) Setting arbitrary application/publisher can be considered spoofing, but a malicious website
        ///       could fake the whole progress page and still achieve the same.
        public string PublisherName
        {
            set
            {
                // Ignore the error code.  This page is transient and it's not the end of the world if this doesn't show up.
                _ = _npp.SetPublisherName(value);
            }

            get { throw new System.NotImplementedException(); }
        }

        /// <SecurityNOoe>
        /// Critical: Calls a SUC'd COM interface method.
        /// TreatAsSafe: Sending even arbitrary progress updates not considered harmful.
        public void UpdateProgress(long bytesDownloaded, long bytesTotal)
        {
            // Ignore the error code.  This page is transient and it's not the end of the world if this doesn't show up.
            _ = _npp.OnDownloadProgress((ulong)bytesDownloaded, (ulong)bytesTotal);
        }

        INativeProgressPage _npp;
    };
}
