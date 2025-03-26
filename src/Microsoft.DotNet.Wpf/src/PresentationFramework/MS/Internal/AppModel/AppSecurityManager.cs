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

using MS.Win32;
using System.Windows;
using MS.Internal.Utility;
using System.Runtime.InteropServices;

namespace MS.Internal.AppModel
{
    internal enum LaunchResult
    {
        Launched,
        NotLaunched,
        NotLaunchedDueToPrompt
    };

    internal static class AppSecurityManager
    {
        ///<summary> 
        ///     Safely launch the browser if you can. 
        ///     If you can't demand unmanaged code permisison. 
        ///
        ///     originatingUri = the current uri
        ///     destinationUri = the uri you are going to. 
        ///</summary> 
        internal static void SafeLaunchBrowserDemandWhenUnsafe(Uri originatingUri, Uri destinationUri, bool fIsTopLevel)
        {
            LaunchResult launched = LaunchResult.NotLaunched;

            launched = SafeLaunchBrowserOnlyIfPossible(originatingUri, destinationUri, fIsTopLevel);
            if (launched == LaunchResult.NotLaunched)
            {
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
        internal static LaunchResult SafeLaunchBrowserOnlyIfPossible(Uri originatingUri, Uri destinationUri, string targetName, bool fIsTopLevel)
        {
            LaunchResult launched = LaunchResult.NotLaunched;

            bool isKnownScheme = (Object.ReferenceEquals(destinationUri.Scheme, Uri.UriSchemeHttp)) ||
                                   (Object.ReferenceEquals(destinationUri.Scheme, Uri.UriSchemeHttps)) ||
                                   destinationUri.IsFile;

            bool fIsMailTo = string.Equals(destinationUri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase);

            // We elevate to navigate the browser iff: 
            //  We are user initiated AND
            //      Scheme == http/https & topLevel OR scheme == mailto.
            //
            // For all other cases ( evil protocols etc). 
            // We will demand. 
            //
            if ((fIsTopLevel && isKnownScheme) || fIsMailTo)
            {
                if (!isKnownScheme && fIsMailTo) // unnecessary if - but being paranoid. 
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
        internal static void UnsafeLaunchBrowser(Uri uri, string targetFrame = null)
        {
            ShellExecuteDefaultBrowser(uri);
        }

        /// <summary>
        /// Opens the default browser for the passed in Uri.
        /// </summary>
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
                throw new InvalidOperationException(SR.FailToLaunchDefaultBrowser,
                    new System.ComponentModel.Win32Exception(/*uses the last Win32 error*/));
        }
    }
}
