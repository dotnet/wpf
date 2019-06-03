// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Description:    AppSecurityManager class.
//
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// IMPORTANT: We are creating an instance of IInternetSecurityManager here. This
// is currently also done in the CustomCredentialPolicy at the Core level. Any
// modification to either of these classes--especially concerning MapUrlToZone--
// should be considered for both classes.
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Interop;
using MS.Internal.Utility;
using MS.Win32;
using System.Runtime.InteropServices;
using MS.Internal.Documents.Application;

using SecurityHelper = MS.Internal.SecurityHelper;

namespace MS.Internal.AppModel
{
    enum LaunchResult
    {
        Launched,
        NotLaunched,
        NotLaunchedDueToPrompt
    };

    internal static class AppSecurityManager
    {
        #region Internal Methods


        ///<summary> 
        ///     Safely launch the browser if you can. 
        ///     If you can't demand unmanaged code permisison. 
        ///
        ///     originatingUri = the current uri
        ///     destinationUri = the uri you are going to. 
        ///</summary> 
        ///<SecurityNote>
        /// Critical - gets access to critical resource (uri), calls critical code (launch browser)
        /// TreatAsSafe because 
        ///                     we consider navigates to http or http to the top-level browser as safe. 
        ///                     we consider navigates to mailto as safe. 
        ///                     for all other cases - we demand Unmanaged Code Permission 
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static void SafeLaunchBrowserDemandWhenUnsafe(Uri originatingUri, Uri destinationUri, bool fIsTopLevel)
        {
            LaunchResult launched = LaunchResult.NotLaunched;

            launched = SafeLaunchBrowserOnlyIfPossible(originatingUri, destinationUri, fIsTopLevel);
            if (launched == LaunchResult.NotLaunched)
            {
                SecurityHelper.DemandUnmanagedCode();
                UnsafeLaunchBrowser(destinationUri);
            }
        }

        ///<summary> 
        /// Safely launch the browser if it's possible to do so in partial trust
        /// Returns enum indicating whether we safely launched ( or at least think we did). 
        ///
        ///     This function is appropriate for use when we launch the browser from partial trust 
        ///     ( as it doesn't perform demands for the "unsafe" cases ) 
        ///</summary> 
        internal static LaunchResult SafeLaunchBrowserOnlyIfPossible(Uri originatingUri, Uri destinationUri, bool fIsTopLevel)
        {
            return SafeLaunchBrowserOnlyIfPossible(originatingUri, destinationUri, null, fIsTopLevel);
        }

        ///<summary> 
        /// Safely launch the browser if it's possible to do so in partial trust
        /// Returns enum indicating whether we safely launched ( or at least think we did). 
        /// Html target names can be passed in with this.
        ///     This function is appropriate for use when we launch the browser from partial trust 
        ///     ( as it doesn't perform demands for the "unsafe" cases ) 
        ///</summary> 
        ///<SecurityNote>
        /// Critical - gets access to critical resource (uri), calls critical code (launch browser)
        ///
        /// TreatAsSafe because 
        ///                     we consider navigates to http or http to the top-level browser as safe. 
        ///                     we consider navigates to mailto as safe. 
        ///
        ///                     for all other cases - we don't launch the browser - and return a result
        ///                     indicating that we didn't launch. 
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static LaunchResult SafeLaunchBrowserOnlyIfPossible(Uri originatingUri, Uri destinationUri, string targetName, bool fIsTopLevel)
        {
            LaunchResult launched = LaunchResult.NotLaunched;

            bool isKnownScheme = (Object.ReferenceEquals(destinationUri.Scheme, Uri.UriSchemeHttp)) ||
                                   (Object.ReferenceEquals(destinationUri.Scheme, Uri.UriSchemeHttps)) ||
                                   destinationUri.IsFile;

            bool fIsMailTo = String.Compare(destinationUri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase) == 0;

            // We elevate to navigate the browser iff: 
            //  We are user initiated AND
            //      Scheme == http/https & topLevel OR scheme == mailto.
            //
            // For all other cases ( evil protocols etc). 
            // We will demand. 
            //
            // The check of IsInitialViewerNavigation is necessary because viewer applications will probably
            // need to call Navigate on the URI they receive, but we want them to be able to do it in partial trust.
            if ((!BrowserInteropHelper.IsInitialViewerNavigation &&
                MS.Internal.PresentationFramework.SecurityHelper.CallerHasUserInitiatedNavigationPermission()) &&
                  ((fIsTopLevel && isKnownScheme) || fIsMailTo))
            {
                if (isKnownScheme)
                {
#if NETFX
                    IBrowserCallbackServices ibcs = ( Application.Current != null ) ? Application.Current.BrowserCallbackServices : null ;
                    if (ibcs != null)
                    {
                        launched = CanNavigateToUrlWithZoneCheck(originatingUri , destinationUri); 
                        if ( launched == LaunchResult.Launched ) 
                        {
                            // resetting launched to NotLaunched here; if the assert succeeds
                            // and ibcs.DelegateNavigation does not throw then we will set it to Launched.
                            launched = LaunchResult.NotLaunched;
                            // Browser app.
                            // We need to see if this is the right behavior when clicking on a link in
                            // a secondary window in a multi-window browser app 
                            ibcs.DelegateNavigation( BindUriHelper.UriToString( destinationUri ), targetName, GetHeaders(destinationUri));   

                            launched = LaunchResult.Launched ; 
                        }                           
                    }
#endif
                }
                else if (fIsMailTo) // unnecessary if - but being paranoid. 
                {
                    // Shell-Exec the browser to the mailto url. 
                    // assumed safe - because we're only allowing this for mailto urls. 
                    // 

                    UnsafeNativeMethods.ShellExecute(new HandleRef(null, IntPtr.Zero), /*hwnd*/
                                                      null, /*operation*/
                                                      BindUriHelper.UriToString(destinationUri), /*file*/
                                                      null, /*parameters*/
                                                      null, /*directory*/
                                                      0); /*nShowCmd*/
                    launched = LaunchResult.Launched;
                }
            }

            return launched;
        }

        // This invokes the browser unsafely.  
        // Whoever is calling this function should do the right demands.
        /// <SecurityNote>
        /// Critical - gets access to critical resource (uri and browsercallback services), calls critical code (launch browser)
        /// </SecurityNote>
        [SecurityCritical]
        internal static void UnsafeLaunchBrowser(Uri uri, string targetFrame = null)
        {
#if NETFX
            // This'll likely go into SafeLaunchBrowser() function.
            if (Application.Current != null && Application.Current.CheckAccess())
            {
                IBrowserCallbackServices ibcs = Application.Current.BrowserCallbackServices;
                if (ibcs != null)
                {
                    // Browser app.
                    // TODO: See if this is the right behavior when clicking on a link in
                    // a secondary window in a multi-window browser app - PS # 840726
                    ibcs.DelegateNavigation(BindUriHelper.UriToString(uri), targetFrame, GetHeaders(uri));
                    return;
                }
            }
#endif

            ShellExecuteDefaultBrowser(uri);
        }

        /// <summary>
        /// Opens the default browser for the passed in Uri.
        /// </summary>
        /// <SecurityNote>
        /// Critical - calls critical code (ShellExecuteEx)
        /// </SecurityNote>
        [SecurityCritical]
        internal static void ShellExecuteDefaultBrowser(Uri uri)
        {
            UnsafeNativeMethods.ShellExecuteInfo sei = new UnsafeNativeMethods.ShellExecuteInfo();
            sei.cbSize = Marshal.SizeOf(sei);
            sei.fMask = UnsafeNativeMethods.ShellExecuteFlags.SEE_MASK_FLAG_DDEWAIT;
            /*
            There is a bug on Windows Vista (with IE 7): ShellExecute via SEE_MASK_CLASSNAME fails for an 
            http[s]:// URL. It works fine for file://. The cause appears to be that the DDE command template
            defined in HKCR\IE.AssocFile.HTM\shell\opennew\ddeexec is used: [file://%1",-1,,,,,]. On XP, the
            the key used is (supposedly) HKCR\htmlfile\shell\opennew\ddeexec, and its value is ["%1",,-1,0,,,,].
            The workaround here is to add the SEE_MASK_CLASSNAME flag only for non-HTTP URLs. For HTTP, 
            "plain" ShellExecute just works, incl. with Firefox/Netscape as the default browser.
            */
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                sei.fMask |= UnsafeNativeMethods.ShellExecuteFlags.SEE_MASK_CLASSNAME;
                sei.lpClass = ".htm"; // The default browser is looked up by this.
            }
            sei.lpFile = uri.ToString(); // It's safe to use Uri.ToString since there's an inheritance demand on it that prevents spoofing by subclasses.
            if (!UnsafeNativeMethods.ShellExecuteEx(sei))
                throw new InvalidOperationException(SR.Get(SRID.FailToLaunchDefaultBrowser),
                    new System.ComponentModel.Win32Exception(/*uses the last Win32 error*/));
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Returns the HTTP "Referer" header. 
        /// </summary>
        /// <returns>returns a string containing one or more HTTP headers separated by \r\n; the string must also be terminated with a \r\n</returns>
        private static string GetHeaders(Uri destinationUri)
        {
            string referer = BindUriHelper.GetReferer(destinationUri);

            if (!String.IsNullOrEmpty(referer))
            {
                // The headers we pass in to IWebBrowser2.Navigate must
                // be terminated with a \r\n because the browser then
                // concatenates its own headers on to the end of that string.
                referer = RefererHeader + referer + "\r\n";
            }

            return referer;
        }

        // 
        // Functionally equivalent copy of Trident's CanNavigateToUrlWithZoneCheck function
        //  Checks to see whether a navigation is considered a zone elevation. 
        //  Once a zone elevation is identified - calls into urlmon to check settings. 
        // 
        ///<SecurityNote> 
        ///     Critical - performs elevations to call IsFeatureEnabled; call critical method MUTZ
        ///
        ///     TreatAsSafe - information disclosed is whether a navigation is "safe" or not. 
        ///                   considered ok to give out. you will be able to get this anyway by trapping exceptions
        ///                   or seeing whether a navigation succeeded/failed. 
        ///</SecurityNote> 
        [SecurityCritical, SecurityTreatAsSafe]
        private static LaunchResult CanNavigateToUrlWithZoneCheck(Uri originatingUri, Uri destinationUri)
        {
            LaunchResult launchResult = LaunchResult.NotLaunched; // fail securely - assume this is the default. 
            int targetZone = NativeMethods.URLZONE_LOCAL_MACHINE; // fail securely this is the most priveleged zone
            int sourceZone = NativeMethods.URLZONE_INTERNET; // fail securely this is the least priveleged zone. 
            bool fEnabled = true;

            EnsureSecurityManager();

            // is this feature enabled ? 
            fEnabled = UnsafeNativeMethods.CoInternetIsFeatureEnabled(
                                        NativeMethods.FEATURE_ZONE_ELEVATION,
                                        NativeMethods.GET_FEATURE_FROM_PROCESS) != NativeMethods.S_FALSE;

            targetZone = MapUrlToZone(destinationUri);

            // Get source zone. 

            // Initialize sourceUri to null so that source zone defaults to the least privileged zone.
            Uri sourceUri = null;

            // If the MimeType is not a container, attempt to find sourceUri.
            // sourceUri should be null for Container cases, since it always assumes
            // the least privileged zone (InternetZone).
            if (Application.Current.MimeType != MimeType.Document)
            {
                sourceUri = BrowserInteropHelper.Source;
            }
            else if (destinationUri.IsFile &&
                System.IO.Path.GetExtension(destinationUri.LocalPath)
                    .Equals(DocumentStream.XpsFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                // In this case we know the following:
                //  1) We are currently a Container
                //  2) The destination is a File and another Container

                // In this case we want to treat the destination as internet too so Container
                // can navigate to other Containers by passing zone checks
                targetZone = NativeMethods.URLZONE_INTERNET;
            }

            if (sourceUri != null)
            {
                sourceZone = MapUrlToZone(sourceUri);
            }
            else
            {
                // 2 potential ways to get here. 
                //      a) We aren't a fusion hosted app. Assume full-trust. 
                //      b) Some bug in hosting caused source Uri to be null. 
                //
                // For a - we will say there is no cross-domain check.     
                //     b - we'll assume InternetZone, and use Source. 

                bool fTrusted = SecurityHelper.CheckUnmanagedCodePermission();

                if (fTrusted)
                {
                    return LaunchResult.Launched;
                }
                else
                {
                    //
                    //  If we didn't get a SourceUri, we'll assume internet zone. 
                    //  And use Source for the uri of origin. 
                    //  
                    //  This isn't quite right - but the sourceUri is only used to show a message to the user. 
                    //  Worse case is confusing user experience. ( this uri is not used in the elevation determination). 
                    //

                    sourceZone = NativeMethods.URLZONE_INTERNET;
                    sourceUri = originatingUri;
                }
            }

            // <Notes from Trident>
            // ------------------------------
            // Check if there is a zone elevation.
            // Custom zones would have a higher value that URLZONE_UNTRUSTED, so this solution isn't quite complete.
            // However, we don't know of any product actively using custom zones, so rolling this out.
            // INTRANET and TRUSTED are treated as equals.
            // ------------------------------
            // </Notes from Trident>

            //
            // Note the negative logic - it first sees to see something is *not* a zone elevation. 
            //
            if (
                    // Even if feature is disabled. 
                    // We still block navigation to local machine. 
                    // IF source zone is internet or restricted. 

                    (!fEnabled &&
                    ((sourceZone != NativeMethods.URLZONE_INTERNET &&
                       sourceZone != NativeMethods.URLZONE_UNTRUSTED) ||
                       targetZone != NativeMethods.URLZONE_LOCAL_MACHINE)) ||

                    // If feature is enabled
                    // It's not a zone elevation if
                    //     the zones are equal OR
                    //        the zones are both less than restricted and 
                    //            the sourceZone is more trusted than Target OR
                    //            sourceZone and TargetZone are both Intranet or Trusted
                    // 
                    // per aganjam - Intranet and Trusted are treated as equivalent 
                    // as it was a common scenario for IE. ( website on intranet points to trusted site). 
                    //

                    (fEnabled &&
                    (
                        sourceZone == targetZone ||
                        (
                            sourceZone <= NativeMethods.URLZONE_UNTRUSTED &&
                            targetZone <= NativeMethods.URLZONE_UNTRUSTED &&
                            (
                                sourceZone < targetZone ||
                                (
                                    (sourceZone == NativeMethods.URLZONE_TRUSTED || sourceZone == NativeMethods.URLZONE_INTRANET) &&
                                    (targetZone == NativeMethods.URLZONE_TRUSTED || targetZone == NativeMethods.URLZONE_INTRANET)
                                )
                            )
                        )
                        )))
            {
                // There is no zone elevation. You can launch away ! 
                return LaunchResult.Launched;
            }

            launchResult = CheckBlockNavigation(sourceUri,
                                                  destinationUri,
                                                  fEnabled);

            return launchResult;
        }

        ///<summary> 
        ///     Called when we suspect there is a zone elevation. 
        ///     Calls the Urlmon IsFeatureZoneElevationEnabled which may pop UI based on settings. 
        /// functionally equivalent to the BlockNavigation: label in Trident's CanNavigateToUrlWithZoneCheck    
        ///</summary> 
        ///<SecurityNote> 
        ///     Critical - calls a function that has a SUC on it. ( CoIntenrnetIsFeatureZoneElevationEnabled)
        ///</SecurityNote> 
        [SecurityCritical]
        private static LaunchResult CheckBlockNavigation(Uri originatingUri, Uri destinationUri, bool fEnabled)
        {
            if (fEnabled)
            {
                if (UnsafeNativeMethods.CoInternetIsFeatureZoneElevationEnabled(
                            BindUriHelper.UriToString(originatingUri),
                            BindUriHelper.UriToString(destinationUri),
                            _secMgr,
                            NativeMethods.GET_FEATURE_FROM_PROCESS) == NativeMethods.S_FALSE)
                {
                    return LaunchResult.Launched;
                }
                else
                {
                    if (IsZoneElevationSettingPrompt(destinationUri))
                    {
                        // url action is query, and we got a "no" answer back. 
                        // we can assume user responded No. 
                        return LaunchResult.NotLaunchedDueToPrompt;
                    }
                    else
                        return LaunchResult.NotLaunched;
                }
            }
            else
            {
                return LaunchResult.Launched;
            }
        }

        // Is ZoneElevation setting set to prompt ?     
        ///<SecurityNote> 
        ///     Critical - elevates to call ProcessUrlAction. 
        ///     TreatAsSafe - information return indicates whether we will prompt for the current zone. 
        ///                   considered ok to expose. 
        ///</SecurityNote> 
        [SecurityCritical, SecurityTreatAsSafe]
        private static bool IsZoneElevationSettingPrompt(Uri target)
        {
            Invariant.Assert(_secMgr != null);

            // Was this due to a prompt ? 

            int policy = NativeMethods.URLPOLICY_DISALLOW;

            unsafe
            {
                String targetString = BindUriHelper.UriToString(target);
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert(); // BlessedAssert: 
                try
                {
                    _secMgr.ProcessUrlAction(targetString,
                                              NativeMethods.URLACTION_FEATURE_ZONE_ELEVATION,
                                              (byte*)&policy,
                                              Marshal.SizeOf(typeof(int)),
                                              null,
                                              0,
                                              NativeMethods.PUAF_NOUI,
                                              0);
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                }
            }

            return (policy == NativeMethods.URLPOLICY_QUERY);
        }

        ///<SecurityNote> 
        /// Critical - elevates to call unmanaged code to set the security site. 
        ///     The SecurityManager is a critical resource.
        /// Safe: The Security Manager is used only within this class (not exposed). Just creating it has 
        ///     no observable side effects.
        ///</SecurityNote> 
        [SecurityCritical, SecurityTreatAsSafe]
        private static void EnsureSecurityManager()
        {
            // IMPORTANT: See comments in header r.e. IInternetSecurityManager

            if (_secMgr == null)
            {
                lock (_lockObj)
                {
                    if (_secMgr == null) // null check again - now that we're in the lock. 
                    {
                        _secMgr = (UnsafeNativeMethods.IInternetSecurityManager)new InternetSecurityManager();

                        //
                        // Set the Security Manager Site. 
                        // This enables any dialogs popped to be modal to our window. 
                        // 
                        _secMgrSite = new SecurityMgrSite();
                        new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert(); // BlessedAssert: 
                        try
                        {
                            _secMgr.SetSecuritySite((NativeMethods.IInternetSecurityMgrSite)_secMgrSite);
                        }
                        finally
                        {
                            CodeAccessPermission.RevertAssert();
                        }
                    }
                }
            }
        }


        ///<SecurityNote> 
        /// Critical - elevates to call SetSecuritySite. 
        /// TreatAsSafe - clearing the security site is considered safe. 
        ///               worse that can happen is any urlmon prompts will be non-modal. 
        ///</SecurityNote> 

        [SecurityCritical, SecurityTreatAsSafe]
        internal static void ClearSecurityManager()
        {
            if (_secMgr != null)
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();  // BlessedAssert: 
                try
                {
                    lock (_lockObj)
                    {
                        if (_secMgr != null)
                        {
                            _secMgr.SetSecuritySite(null);
                            _secMgrSite = null;
                            _secMgr = null;
                        }
                    }
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                }
            }
        }

        ///<SecurityNote> 
        /// Critical - Calls the COM method. A URL's security zone is not a big secret, and in most cases it 
        ///     can be inferred by just parsing the URL, but it's still information obtained under elevation,
        ///     and thus we shouldn't leak it without a good reason.
        ///</SecurityNote> 
        [SecurityCritical]
        internal static int MapUrlToZone(Uri url)
        {
            EnsureSecurityManager();
            int zone;
            _secMgr.MapUrlToZone(BindUriHelper.UriToString(url), out zone, 0);
            return zone;
        }

        [ComImport, ComVisible(false), Guid("7b8a2d94-0ac9-11d1-896c-00c04Fb6bfc4")]
        internal class InternetSecurityManager
        {
        }



        #endregion Private Methods

        #region Private Fields

        private const string RefererHeader = "Referer: ";

        private const string BrowserOpenCommandLookupKey = "htmlfile\\shell\\open\\command";

        // Object to be used for locking.  Using typeof(Util) causes an FxCop
        // violation DoNotLockOnObjectsWithWeakIdentity
        private static object _lockObj = new object();

        ///<SecurityNote> 
        ///     Critical - requires an elevation to create. 
        ///</SecurityNote> 
        [SecurityCritical]
        private static UnsafeNativeMethods.IInternetSecurityManager _secMgr;

        private static SecurityMgrSite _secMgrSite;

        #endregion Private Fields
    }
}
