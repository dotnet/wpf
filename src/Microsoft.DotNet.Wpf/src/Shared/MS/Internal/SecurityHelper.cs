// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Helper functions that require elevation but are safe to use.
*
\***************************************************************************/

// The SecurityHelper class differs between assemblies and could not actually be
//  shared, so it is duplicated across namespaces to prevent name collision.
// This duplication seems hardly necessary now. We should continue
// trying to reduce it by pushing things from Framework to Core (whenever it makes sense).
#if WINDOWS_BASE
namespace MS.Internal.WindowsBase
#elif PRESENTATION_CORE
using MS.Internal.PresentationCore;
namespace MS.Internal // Promote the one from PresentationCore as the default to use.
#elif PRESENTATIONFRAMEWORK
namespace MS.Internal.PresentationFramework
#elif PBTCOMPILER
namespace MS.Internal.PresentationBuildTasks
#elif REACHFRAMEWORK
namespace MS.Internal.ReachFramework
#elif DRT
namespace MS.Internal.Drt
#else
#error Class is being used from an unknown assembly.
#endif
{
    using System;
    using System.Globalization;     // CultureInfo
    using System.Security;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using System.Diagnostics.CodeAnalysis;


#if !PBTCOMPILER
    using MS.Win32;
    using System.IO.Packaging;
#endif

#if PRESENTATION_CORE
using MS.Internal.AppModel;
#endif

#if PRESENTATIONFRAMEWORK_ONLY
    using System.Diagnostics;
    using System.Windows;
    using MS.Internal.Utility;      // BindUriHelper
    using MS.Internal.AppModel;
#endif

#if REACHFRAMEWORK
    using MS.Internal.Utility;
#endif
#if WINDOWS_BASE
    // This existed originally to allow FontCache service to 
    // see the WindowsBase variant of this class. We no longer have
    // a FontCache service, but over time other parts of WPF might
    // have started to depend on this, so we leave it as-is for 
    // compat. 
    [FriendAccessAllowed] 
#endif


internal static class SecurityHelper
    {

#if PRESENTATION_CORE

        internal static Uri GetBaseDirectory(AppDomain domain)
        {
            Uri appBase = null;
            appBase = new Uri(domain.BaseDirectory);
            return( appBase );
        }

         internal static int MapUrlToZoneWrapper(Uri uri)
         {
              int targetZone = NativeMethods.URLZONE_LOCAL_MACHINE ; // fail securely this is the most priveleged zone
              int hr = NativeMethods.S_OK ;
              object curSecMgr = null;
              hr = UnsafeNativeMethods.CoInternetCreateSecurityManager(
                                                                       null,
                                                                       out curSecMgr ,
                                                                       0 );
              if ( NativeMethods.Failed( hr ))
                  throw new Win32Exception( hr ) ;

              UnsafeNativeMethods.IInternetSecurityManager pSec = (UnsafeNativeMethods.IInternetSecurityManager) curSecMgr;

              string uriString = BindUriHelper.UriToString( uri ) ;
              //
              // special case the condition if file is on local machine or UNC to ensure that content with mark of the web
              // does not yield with an internet zone result
              //
              if (uri.IsFile)
              {
                  pSec.MapUrlToZone( uriString, out targetZone, MS.Win32.NativeMethods.MUTZ_NOSAVEDFILECHECK );
              }
              else
              {
                  pSec.MapUrlToZone( uriString, out targetZone, 0 );
              }
              //
              // This is the condition for Invalid zone
              //
              if (targetZone < 0)
              {
                throw new SecurityException( SR.Invalid_URI );
              }
              pSec = null;
              curSecMgr = null;
              return targetZone;
        }
#endif


#if WINDOWS_BASE
        internal static void RunClassConstructor(Type t)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
        }

#endif //  WINDOWS_BASE

#if DRT
        /// <remarks> The LinkDemand on Marshal.SizeOf() was removed in v4. </remarks>
        internal static int SizeOf(Type t)
        {
            return Marshal.SizeOf(t);
        }
#endif

#if DRT
        internal static int SizeOf(Object o)
        {
            return Marshal.SizeOf(o);
        }
#endif

#if WINDOWS_BASE || PRESENTATION_CORE || PRESENTATIONFRAMEWORK
        internal static Exception GetExceptionForHR(int hr)
        {
            return Marshal.GetExceptionForHR(hr, new IntPtr(-1));
        }
#endif

#if WINDOWS_BASE || PRESENTATION_CORE
        internal static void ThrowExceptionForHR(int hr)
        {
            Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
        }
        
        internal static int GetHRForException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            // GetHRForException fills a per thread IErrorInfo object with data from the exception
            // The exception may contain security sensitive data like full file paths that we do not
            // want to leak into an IErrorInfo
            int hr = Marshal.GetHRForException(exception);

            // Call GetHRForException a second time with a security safe exception object
            // to make sure the per thread IErrorInfo is cleared of security sensitive data
            Marshal.GetHRForException(new Exception());

            return hr;
        }

#endif

#if PRESENTATIONFRAMEWORK

        /// <summary>
        /// A helper method to do the necessary work to display a standard MessageBox.  This method performs
        /// and necessary elevations to make the dialog work as well.
        /// </summary>
        internal
        static
        void
        ShowMessageBoxHelper(
            System.Windows.Window parent,
            string text,
            string title,
            System.Windows.MessageBoxButton buttons,
            System.Windows.MessageBoxImage image
            )
        {
            // if we have a known parent window set, let's use it when alerting the user.
            if (parent != null)
            {
                System.Windows.MessageBox.Show(parent, text, title, buttons, image);
            }
            else
            {
                System.Windows.MessageBox.Show(text, title, buttons, image);
            }
        }
        /// <summary>
        /// A helper method to do the necessary work to display a standard MessageBox.  This method performs
        /// and necessary elevations to make the dialog work as well.
        /// </summary>
        internal
        static
        void
        ShowMessageBoxHelper(
            IntPtr parentHwnd,
            string text,
            string title,
            System.Windows.MessageBoxButton buttons,
            System.Windows.MessageBoxImage image
            )
        {
            // NOTE: the last param must always be MessageBoxOptions.None for this to be considered TreatAsSafe
            System.Windows.MessageBox.ShowCore(parentHwnd, text, title, buttons, image, MessageBoxResult.None, MessageBoxOptions.None);
        }
#endif

#if PRESENTATION_CORE || PRESENTATIONFRAMEWORK || WINDOWS_BASE

        internal static bool AreStringTypesEqual(string m1, string m2)
        {
            return (string.Equals(m1, m2, StringComparison.OrdinalIgnoreCase));
        }

#endif //PRESENTATION_CORE || PRESENTATIONFRAMEWORK || WINDOWS_BASE


#if WINDOWS_BASE
        ///
        /// Read and return a registry value.
       static internal object ReadRegistryValue( RegistryKey baseRegistryKey, string keyName, string valueName )
       {
            object value = null;

            RegistryKey key = baseRegistryKey.OpenSubKey(keyName);
            if (key != null)
            {
                using( key )
                {
                    value = key.GetValue(valueName);
                }
            }

            return value;
        }
#endif // WINDOWS_BASE
}
}

