// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:  
//      WebBrowserEvent is used to listen to the DWebBrowserEvent2
//      of the webbrowser control
//
//      Copied from WebBrowse.cs in winforms
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Text;
using System.Windows.Navigation;
using MS.Internal.PresentationFramework;
using System.Windows.Controls;
using MS.Win32;
using MS.Internal.AppModel;
using MS.Internal.Interop;

//In order to avoid generating warnings about unknown message numbers and 
//unknown pragmas when compiling your C# source code with the actual C# compiler, 
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace MS.Internal.Controls
{
    /// <SecurityNote>
    /// 1) By virtue of being derived from InternalDispatchObject, WebBrowserEvent objects are unsafe to expose
    ///     in partial trust--in principle. Having an object reference, partial-trust code would be able to 
    ///     call the methods implementing DWebBrowserEvents2. Since most of our current implementation methods
    ///     are SecurityTreatAsSafe, this exposure concern is insignificant.
    /// 2) WebOCHostedInBrowserProcess - defense in depth: The DWebBrowserEvents2 implementation is exposed 
    ///     across a security boundary. We must not allow a compromised low-integrity-level browser process to
    ///     gain elevation of privilege via our process or tamper with its state. (Attacking the WebOC via this 
    ///     interface is not interesting, because the WebOC is directly accessible in the browser process.)
    ///
    ///     Normally this type and all its methods would be critical however the type derives from InternalDispatchObject.
    ///     InternalDispatchObject implements the public transparent IReflect interface and via that interface 
    ///     all the other methods on this type are (by design) publicly accessible. Marking them critical causes 
    ///     the security subsystem to inject an UnmanagedCode demand that will not be satified in partial trust.
    /// </SecurityNote>
    [ClassInterface(ClassInterfaceType.None)]
    internal class WebBrowserEvent :
        InternalDispatchObject<UnsafeNativeMethods.DWebBrowserEvents2>,
        UnsafeNativeMethods.DWebBrowserEvents2
    {
        private WebBrowser _parent;

        /// <SecurityNote>
        /// Critical because the base class ctor is critical. 
        /// Instances should not be exposed in partial trust, unless all methods implementing DWebBrowserEvents2
        /// are safe to call.
        /// </SecurityNote>
        [SecurityCritical]
        public WebBrowserEvent(WebBrowser parent)
        {
            _parent = parent;
            Debug.Assert(parent != null, "WebBrowser control required for hooking webbrowser events");
        }

        /// <SecurityNote>
        /// Critical: This code extracts the IWebBrowser2 interface
        /// TreatAsSafe: This does not expose the interface, also calling this will not cause a navigation
        ///     It simply enforces a few checks.
        /// WebOCHostedInBrowserProcess: By passing appropriately crafted parameters, malicious code in the 
        ///     low-integrity browser process could get us to set the SecurityCritical _parent.NavigatingToAboutBlank 
        ///     to false. This is currently safe. (But setting it to true is not!)
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public void BeforeNavigate2(object pDisp, ref object url, ref object flags, ref object targetFrameName, ref object postData, ref object headers, ref bool cancel)
        {
            bool newNavigationInitiated = false;
            bool cancelRequested = false;

            try
            {
                Debug.Assert(url == null || url is string, "invalid url type");
                Debug.Assert(targetFrameName == null || targetFrameName is string, "invalid targetFrameName type");
                Debug.Assert(headers == null || headers is string, "invalid headers type");
                //
                // Due to a bug in the interop code where the variant.bstr value gets set
                // to -1 on return back to native code, if the original value was null, we
                // have to set targetFrameName and headers to "".
                if (targetFrameName == null)
                {
                    targetFrameName = "";
                }
                if (headers == null)
                {
                    headers = "";
                }

                string urlString = (string)url;
                Uri source = String.IsNullOrEmpty(urlString) ? null : new Uri(urlString);

                UnsafeNativeMethods.IWebBrowser2 axIWebBrowser2 = (UnsafeNativeMethods.IWebBrowser2)pDisp;
                // If _parent.AxIWebBrowser2 != axIWebBrowser2, navigation happens in a nested [i]frame.
                // in that case we do not want to enforce site locking as we want the default IE behavior to take 
                // over. 
                if (_parent.AxIWebBrowser2 == axIWebBrowser2)
                {
                    // The NavigatingToAboutBlank property indicates whether we are navigating to "about:blank"
                    // as a result of navigating to null or stream/string navigation.
                    // We set the NavigatingToAboutBlank bit to true in the WebBrowser DoNavigate method. When the above
                    // conditions occur, the NavigatingToAboutBlank is true and the source must be "about:blank". 
                    // 
                    // But when end user navigates away from the current about:blank page (by clicking
                    // on a hyperlink, Goback/Forward), or programmatically call GoBack and Forward,
                    // When we get the navigating event, NavigatingToAboutBlank is true, but the source is not "about:blank".
                    // Clear the NavigatingToAboutBlank bit in that case.
                    if ((_parent.NavigatingToAboutBlank) &&
                         String.Compare(urlString, WebBrowser.AboutBlankUriString, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        _parent.NavigatingToAboutBlank = false;
                    }

                    // Site locking for top level WebOC navigation
                    // "about:blank is not enabled in partial trust publicly. 
                    // We enable it internally to navigate to null. 
                    if ((!_parent.NavigatingToAboutBlank) &&
                        !SecurityHelper.CallerHasWebPermission(source) &&
                        !IsAllowedScriptScheme(source))
                    {
                        cancelRequested = true;
                    }
                    else
                    {
                        // When source set to null or navigating to stream/string, we navigate to "about:blank"
                        // internally. Make sure we pass null in the event args. 
                        if (_parent.NavigatingToAboutBlank)
                        {
                            source = null;
                        }

                        NavigatingCancelEventArgs e = new NavigatingCancelEventArgs(source,
                                                  null, null, null, NavigationMode.New, null, null, true);

                        // Launching a navigation from the Navigating event handler causes reentrancy.
                        // For more info, see WebBrowser.LastNavigation. This is a point of possible reentrancy. Whenever
                        // a new navigation is started during the call to the Navigating event handler, we need to cancel
                        // out the current navigation.
                        Guid lastNavigation = _parent.LastNavigation;

                        // Fire navigating event. Events are only fired for top level navigation. 
                        _parent.OnNavigating(e);

                        // Launching a navigation from the Navigating event handler causes reentrancy.
                        // For more info, see WebBrowser.LastNavigation. If _lastNavigation has changed during the call to
                        // the event handlers for Navigating, we know a new navigation has been initialized.
                        if (_parent.LastNavigation != lastNavigation)
                        {
                            newNavigationInitiated = true;
                        }

                        cancelRequested = e.Cancel;
                    }
                }
            }
            // We disable this to suppress FXCop warning since in this case we really want to catch all exceptions
            // please refer to comment below
#pragma warning disable 6502
            catch
            {
                // This is an interesting pattern of putting a try catch block around this that catches everything,
                // The reason I do this is based on a conversation with Changov. What happens here is if there is 
                // an exception in any of the code above then navigation still continues since COM interop eats up 
                // the exception. But what we want is for this navigation to fail.
                // There fore I catch all exceptions and cancel navigation 
                cancelRequested = true;
            }
#pragma warning restore 6502
            finally
            {
                // Clean the WebBrowser control state if navigation cancelled.
                //
                // Launching a navigation from the Navigating event handler causes reentrancy.
                // For more info, see WebBrowser.LastNavigation. A new navigation started during event handling for
                // the Navigating event will have touched the control's global state. In case the navigation was
                // cancelled in order to start a new navigation, we shouldn't tamper with that new state.
                if (cancelRequested && !newNavigationInitiated)
                {
                    _parent.CleanInternalState();
                }

                // Launching a navigation from the Navigating event handler causes reentrancy.
                // For more info, see WebBrowser.LastNavigation. Whenever a new navigation is started during the
                // call to the Navigating event handler, we need to cancel out the current navigation.
                if (cancelRequested || newNavigationInitiated)
                {
                    cancel = true;
                }
            }
        }

        /// <summary>
        /// Determines whether a URI has a recognized and allowed URI scheme that we shouldn't block on navigation
        /// attempts from the browser, regardless of whether we are partial trust or not.
        /// <SecurityNote>
        ///     Critical: Can be used to make a security decision about script execution.
        ///     TreatAsSafe: Does not perform privileged operations; also usable for non-security related stuff.
        /// </SecurityNote>
        /// </summary>
        [SecurityCritical, SecurityTreatAsSafe]
        private static bool IsAllowedScriptScheme(Uri uri)
        {
            return uri != null && (uri.Scheme == "javascript" || uri.Scheme == "vbscript");
        }

        /// <summary>
        ///     Critical: This code extracts the IWebBrowser2, IHTMLDocument interface.
        ///     TreatAsSafe: This does not expose the interface.
        /// </summary>
        [SecurityCritical, SecurityTreatAsSafe]
        public void NavigateComplete2(object pDisp, ref object url)
        {
            Debug.Assert(url == null || url is string, "invalid url type");

            // Events only fired for top level navigation. 
            UnsafeNativeMethods.IWebBrowser2 axIWebBrowser2 = (UnsafeNativeMethods.IWebBrowser2)pDisp;
            if (_parent.AxIWebBrowser2 == axIWebBrowser2)
            {
                // If we are loading from stream.                
                if (_parent.DocumentStream != null)
                {
                    Invariant.Assert(_parent.NavigatingToAboutBlank &&
                        (String.Compare((string)url, WebBrowser.AboutBlankUriString, StringComparison.OrdinalIgnoreCase) == 0));

                    try
                    {
                        UnsafeNativeMethods.IHTMLDocument nativeHTMLDocument = _parent.NativeHTMLDocument;
                        if (nativeHTMLDocument != null)
                        {
                            UnsafeNativeMethods.IPersistStreamInit psi = nativeHTMLDocument as UnsafeNativeMethods.IPersistStreamInit;
                            Debug.Assert(psi != null, "The Document does not implement IPersistStreamInit");

                            System.Runtime.InteropServices.ComTypes.IStream iStream =
                                new MS.Internal.IO.Packaging.ManagedIStream(_parent.DocumentStream);

                            psi.Load(iStream);
                        }
                    }
                    finally
                    {
                        _parent.DocumentStream = null;
                    }
                }
                else
                {
                    string urlString = (string)url;
                    // When source set to null or navigating to stream/string, we navigate to "about:blank"
                    // internally. Make sure we pass null in the event args. 
                    if (_parent.NavigatingToAboutBlank)
                    {
                        Invariant.Assert(String.Compare(urlString, WebBrowser.AboutBlankUriString, StringComparison.OrdinalIgnoreCase) == 0);
                        urlString = null;
                    }
                    Uri source = (String.IsNullOrEmpty(urlString) ? null : new Uri(urlString));
                    NavigationEventArgs e = new NavigationEventArgs(source, null, null, null, null, true);

                    _parent.OnNavigated(e);
                }
            }
        }

        /// <summary>
        ///     Critical: This code accesses the IWebBrowser2, IHTMLDocument interface.
        ///     TreatAsSafe: This does not expose the interface.
        /// </summary>
        [SecurityCritical, SecurityTreatAsSafe]
        public void DocumentComplete(object pDisp, ref object url)
        {
            Debug.Assert(url == null || url is string, "invalid url type");

            // Events only fired for top level navigation. 
            UnsafeNativeMethods.IWebBrowser2 axIWebBrowser2 = (UnsafeNativeMethods.IWebBrowser2)pDisp;
            if (_parent.AxIWebBrowser2 == axIWebBrowser2)
            {
                string urlString = (string)url;
                // When source set to null or navigating to stream/string, we navigate to "about:blank"
                // internally. Make sure we pass null in the event args. 
                if (_parent.NavigatingToAboutBlank)
                {
                    Invariant.Assert(String.Compare(urlString, WebBrowser.AboutBlankUriString, StringComparison.OrdinalIgnoreCase) == 0);
                    urlString = null;
                }
                Uri source = (String.IsNullOrEmpty(urlString) ? null : new Uri(urlString));
                NavigationEventArgs e = new NavigationEventArgs(source, null, null, null, null, true);

                _parent.OnLoadCompleted(e);
            }
        }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void CommandStateChange(long command, bool enable)
        {
            if (command == NativeMethods.CSC_NAVIGATEBACK)
            {
                _parent._canGoBack = enable;
            }
            else if (command == NativeMethods.CSC_NAVIGATEFORWARD)
            {
                _parent._canGoForward = enable;
            }
        }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void TitleChange(string text)
        {
            //this.parent.OnDocumentTitleChanged(EventArgs.Empty);
        }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void SetSecureLockIcon(int secureLockIcon)
        {
            //this.parent.encryptionLevel = (WebBrowserEncryptionLevel)secureLockIcon;
            //this.parent.OnEncryptionLevelChanged(EventArgs.Empty);
        }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void NewWindow2(ref object ppDisp, ref bool cancel)
        {
            //CancelEventArgs e = new CancelEventArgs();
            //this.parent.OnNewWindow(e);
            //cancel = e.Cancel;
        }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void ProgressChange(int progress, int progressMax)
        {
            //WebBrowserProgressChangedEventArgs e = new WebBrowserProgressChangedEventArgs(progress, progressMax);
            //this.parent.OnProgressChanged(e);
        }

        /// <SecurityNote>
        /// Critical: Setting the browser's status bar allows hyperlink URL spoofing.
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// WebOCHostedInBrowserProcess: Setting the status bar through this method is not interesting, because
        ///     malicious code in the browser process could do this directly.
        /// </SecurityNote>
        [SecurityCritical]
        public void StatusTextChange(string text)
        {
            _parent.RaiseEvent(new RequestSetStatusBarEventArgs(text));
        }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void DownloadBegin()
        {
            //this.parent.OnFileDownload(EventArgs.Empty);
        }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void FileDownload(ref bool activeDocument, ref bool cancel) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void PrivacyImpactedStateChange(bool bImpacted) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void UpdatePageStatus(object pDisp, ref object nPage, ref object fDone) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void PrintTemplateTeardown(object pDisp) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void PrintTemplateInstantiation(object pDisp) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void NavigateError(object pDisp, ref object url, ref object frame, ref object statusCode, ref bool cancel) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void ClientToHostWindow(ref long cX, ref long cY) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void WindowClosing(bool isChildWindow, ref bool cancel) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void WindowSetHeight(int height) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void WindowSetWidth(int width) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void WindowSetTop(int top) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void WindowSetLeft(int left) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void WindowSetResizable(bool resizable) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void OnTheaterMode(bool theaterMode) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void OnFullScreen(bool fullScreen) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void OnStatusBar(bool statusBar) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void OnMenuBar(bool menuBar) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void OnToolBar(bool toolBar) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void OnVisible(bool visible) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void OnQuit() { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void PropertyChange(string szProperty) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void DownloadComplete() { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void SetPhishingFilterStatus(uint phishingFilterStatus) { }

        /// <SecurityNote>
        /// Critical: Implements critical interface (UnsafeNativeMethods.DWebBrowserEvents2) member
        /// NOT PublicOK! The WebBrowserEvent object should not be exposed to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        public void WindowStateChanged(uint dwFlags, uint dwValidFlagsMask) { }
    }
}
