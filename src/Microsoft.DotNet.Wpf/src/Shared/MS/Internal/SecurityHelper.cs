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
    using System.Security.Permissions;
    using System.Net; // WebPermission.
    using System.ComponentModel;
    using System.Security.Policy;
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

        //This code path is only executed if we are trying to get to http content from https
        internal static Uri ExtractUriForClickOnceDeployedApp()
        {
            // This api returns the location from where an app was deployed. In case of browser hosted apps
            // there are no elevations and this information is safe to return.
            // In case of non browserhosted scenarios this will trigger a demand since we do not assert to get
            // this information in the code below
            return    SiteOfOriginContainer.SiteOfOriginForClickOnceApp;
        }

         //This code path is only executed if we are trying to get to http content from https
        internal static void BlockCrossDomainForHttpsApps(Uri uri)
        {
            // if app is HTTPS, no cross domain allowed
            Uri appDeploymentUri = ExtractUriForClickOnceDeployedApp();
            if (appDeploymentUri != null && appDeploymentUri.Scheme == Uri.UriSchemeHttps)
            {
                // demand
                if (uri.IsUnc || uri.IsFile)
                {
                    (new FileIOPermission(FileIOPermissionAccess.Read, uri.LocalPath)).Demand();
                }
                else
                {
                    (new WebPermission(NetworkAccess.Connect, BindUriHelper.UriToString(uri))).Demand();
                }
            }
        }

        // EnforceUncContentAccessRules implements UNC media & imaging access rules
        internal static void EnforceUncContentAccessRules(Uri contentUri)
        {
            // this should be called only for UNC content
            Invariant.Assert(contentUri.IsUnc);

            // get app zone and scheme
            Uri appUri = SecurityHelper.ExtractUriForClickOnceDeployedApp();
            if( appUri == null )
            {
                // we are not in a browser hosted app; we are not in partial trust, so don't block
                return;
            }

            // get app's zone
            int appZone = SecurityHelper.MapUrlToZoneWrapper(appUri);

            // demand if
            // 1) app comes from Internet or a more untrusted zone, or
            // 2) app comes from Intranet and scheme is HTTPS
            bool isInternetOrLessTrustedApp = (appZone >= MS.Win32.NativeMethods.URLZONE_INTERNET);
            bool isIntranetHttpsApp = (appZone == MS.Win32.NativeMethods.URLZONE_INTRANET && appUri.Scheme == Uri.UriSchemeHttps);
            if (isInternetOrLessTrustedApp || isIntranetHttpsApp)
            {
                // demand appropriate permission - we already know that contentUri is Unc
                (new FileIOPermission(FileIOPermissionAccess.Read, contentUri.LocalPath)).Demand();
            }
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
                throw new SecurityException( SR.Get(SRID.Invalid_URI) );
              }
              pSec = null;
              curSecMgr = null;
              return targetZone;
        }

        internal static PermissionSet ExtractAppDomainPermissionSetMinusSiteOfOrigin()
        {
            return new PermissionSet(PermissionState.Unrestricted);
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

        // don't include this in the compiler - avoid compiler changes when we can.
#if !PBTCOMPILER
        internal static void DemandUnrestrictedUIPermission()
        {
            if(_unrestrictedUIPermission == null)
            {
                _unrestrictedUIPermission = new UIPermission(PermissionState.Unrestricted);
            }
            _unrestrictedUIPermission.Demand();
        }
        static UIPermission _unrestrictedUIPermission = null;
#endif

#if PRESENTATION_CORE
        internal static void DemandFileIOReadPermission(string fileName)
        {
            new FileIOPermission(FileIOPermissionAccess.Read, fileName).Demand();
        }
#endif

#if NEVER
        internal static void DemandFileDialogSavePermission()
        {
            if(_fileDialogSavePermission == null)
            {
                _fileDialogSavePermission = new FileDialogPermission(FileDialogPermissionAccess.Save);
            }
            _fileDialogSavePermission.Demand();
        }
        static FileDialogPermission _fileDialogSavePermission = null;
#endif

#if PRESENTATIONFRAMEWORK

        internal static void DemandUnrestrictedFileIOPermission()
        {
            if(_unrestrictedFileIOPermission == null)
            {
                _unrestrictedFileIOPermission = new FileIOPermission(PermissionState.Unrestricted);
            }
            _unrestrictedFileIOPermission.Demand();
        }
        static FileIOPermission _unrestrictedFileIOPermission = null;

        internal static void DemandFileDialogOpenPermission()
        {
            if(_fileDialogOpenPermission == null)
            {
                _fileDialogOpenPermission = new FileDialogPermission(FileDialogPermissionAccess.Open);
            }
            _fileDialogOpenPermission.Demand();
        }
        static FileDialogPermission _fileDialogOpenPermission = null;

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
            (new SecurityPermission(SecurityPermissionFlag.UnmanagedCode)).Assert();
            try
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
            finally
            {
                SecurityPermission.RevertAssert();
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
            (new SecurityPermission(SecurityPermissionFlag.UnmanagedCode)).Assert();
            try
            {
                // NOTE: the last param must always be MessageBoxOptions.None for this to be considered TreatAsSafe
                System.Windows.MessageBox.ShowCore(parentHwnd, text, title, buttons, image, MessageBoxResult.None, MessageBoxOptions.None);
            }
            finally
            {
                SecurityPermission.RevertAssert();
            }
        }
#endif


#if PRESENTATION_CORE || REACHFRAMEWORK
        internal static void DemandMediaAccessPermission(String uri)
        {
            CodeAccessPermission casPermission= SecurityHelper.CreateMediaAccessPermission(uri);
            if(casPermission != null)
            {
                casPermission.Demand();
            }
        }
#endif

#if PRESENTATION_CORE || REACHFRAMEWORK
        internal
        static
        CodeAccessPermission
        CreateMediaAccessPermission(String uri)
        {
            CodeAccessPermission codeAccessPermission = null;
            if (uri != null)
            {
                // do a Case invariant dotnet culture specific string compare
                if (String.Compare(SafeSecurityHelper.IMAGE, uri, true/*Ignore case*/, System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS ) == 0)
                {
                    codeAccessPermission = new MediaPermission(MediaPermissionAudio.NoAudio,
                                                               MediaPermissionVideo.NoVideo,
                                                               MediaPermissionImage.AllImage);
                }
                else
                {
                    // we allow access to pack: bits so assuming scheme is not pack: we demand
                    if (String.Compare((System.Windows.Navigation.BaseUriHelper.GetResolvedUri(System.Windows.Navigation.BaseUriHelper.BaseUri, new Uri(uri, UriKind.RelativeOrAbsolute))).Scheme,
                                       PackUriHelper.UriSchemePack, true /* ignore case */,
                                       System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS) != 0)
                    {
                        // Creating a URI is fine it is going the other way that is risky
                        if(!SecurityHelper.CallerHasWebPermission(new Uri(uri,UriKind.RelativeOrAbsolute)))
                        {
                            codeAccessPermission = new MediaPermission(MediaPermissionAudio.NoAudio,
                                                                       MediaPermissionVideo.NoVideo,
                                                                       MediaPermissionImage.AllImage);
                        }

                    }
                }
            }
            else
            {
                codeAccessPermission = new MediaPermission(MediaPermissionAudio.NoAudio,
                                                           MediaPermissionVideo.NoVideo,
                                                           MediaPermissionImage.AllImage);
            }
            return codeAccessPermission;
        }

        ///<summary>
        ///   Check caller has web-permission. for a given Uri.
        /// </summary>
#if REACHFRAMEWORK        
#else        
#endif        
        internal static bool CallerHasWebPermission( Uri uri )
        {
            try
            {
                SecurityHelper.DemandWebPermission(uri);
                return true;
            }
            catch ( SecurityException )
            {
                return false ;
            }
        }

        internal static void DemandWebPermission( Uri uri )
        {
            // We do this first as a security measure since the call below
            // checks for derivatives. Please note we need to extract the
            // string to call into WebPermission anyways, the only thing that
            // doing this early gains us is a defense in depth measure. The call
            // is required nevertheless.
            string finalUri = BindUriHelper.UriToString( uri );

            if (uri.IsFile)
            {
                // If the scheme is file: demand file io
                string toOpen = uri.LocalPath;
                (new FileIOPermission(FileIOPermissionAccess.Read, toOpen)).Demand();
            }
            else
            {
                // else demand web permissions
                new WebPermission(NetworkAccess.Connect, finalUri).Demand();
            }
        }

#endif //PRESENTATIONCORE||REACHFRAMEWORK

#if PRESENTATION_CORE || PRESENTATIONFRAMEWORK || REACHFRAMEWORK
        /// <summary>
        /// By default none of the plug-in serializer code must succeed for partially trusted callers
        /// </summary>
        internal static void DemandPlugInSerializerPermissions()
        {
            if(_plugInSerializerPermissions == null)
            {
                _plugInSerializerPermissions = new PermissionSet(PermissionState.Unrestricted);
            }
            _plugInSerializerPermissions.Demand();
        }
        static PermissionSet _plugInSerializerPermissions = null;
#endif //PRESENTATIONFRAMEWORK

#if PRESENTATION_CORE || PRESENTATIONFRAMEWORK || WINDOWS_BASE

        internal static bool AreStringTypesEqual(string m1, string m2)
        {
            return (String.Compare(m1, m2, StringComparison.OrdinalIgnoreCase) == 0);
        }

#endif //PRESENTATION_CORE || PRESENTATIONFRAMEWORK || WINDOWS_BASE


#if WINDOWS_BASE
        ///
        /// Read and return a registry value.
       static internal object ReadRegistryValue( RegistryKey baseRegistryKey, string keyName, string valueName )
       {
            object value = null;

            new RegistryPermission(RegistryPermissionAccess.Read, baseRegistryKey.Name + @"\" + keyName).Assert();
            try
            {
                RegistryKey key = baseRegistryKey.OpenSubKey(keyName);
                if (key != null)
                {
                    using( key )
                    {
                        value = key.GetValue(valueName);
                    }
                }
            }
            finally
            {
                RegistryPermission.RevertAssert();
            }

            return value;
}
#endif // WINDOWS_BASE
}
}

