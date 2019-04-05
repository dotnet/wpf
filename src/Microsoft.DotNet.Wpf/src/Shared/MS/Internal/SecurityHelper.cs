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

#if WINDOWS_BASE || PRESENTATIONUI
    using MS.Internal.Permissions;
#endif
#if PRESENTATION_CORE
using MS.Internal.AppModel;
using MS.Internal.Permissions;
#endif

#if PRESENTATIONFRAMEWORK_ONLY
    using MS.Internal.Permissions ;
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
#if !PBTCOMPILER
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
#if REACHFRAMEWORK
        [SecurityCritical]
#else        
        [SecuritySafeCritical]
#endif        
        internal static bool CheckUnmanagedCodePermission()
        {
            try
            {
                SecurityHelper.DemandUnmanagedCode();
            }
            catch(SecurityException )
            {
                return false ;
            }

            return true;
        }

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandUnmanagedCode()
        {
            if(_unmanagedCodePermission == null)
            {
                _unmanagedCodePermission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
            }
            _unmanagedCodePermission.Demand();
        }
        static SecurityPermission _unmanagedCodePermission = null;
#endif  //  !PBTCOMPILER

#if PRESENTATION_CORE

        ///<summary>
        /// Create a UserInitiatedRoutedEvent permission.
        /// Separate helper exists to make it easy to change what the permission is.
        ///</summary>
        ///<SecurityNote>
        ///  Critical: Returns a permission object, which can be misused.
        ///</SecurityNote>
        [SecurityCritical]
        internal static CodeAccessPermission CreateUserInitiatedRoutedEventPermission()
        {
            if(_userInitiatedRoutedEventPermission == null)
            {
                _userInitiatedRoutedEventPermission = new UserInitiatedRoutedEventPermission();
            }
            return _userInitiatedRoutedEventPermission;
        }

        ///<summary>
        /// Check whether the call stack has the permissions needed for UserInitiated RoutedEvents.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CallerHasUserInitiatedRoutedEventPermission()
        {
            try
            {
                CreateUserInitiatedRoutedEventPermission().Demand();
            }
            catch (SecurityException)
            {
                return false;
            }
            return true;
        }

        static UserInitiatedRoutedEventPermission _userInitiatedRoutedEventPermission = null;

#endif // PRESENTATION_CORE

#if PRESENTATIONFRAMEWORK

        /// <SecurityNote>
        /// Critical: This code throws an exception if someone sets value to be true and we are in partial trust
        /// TreatAsSafe: The only reason we mark this as critical is to track code
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static void ThrowExceptionIfSettingTrueInPartialTrust(ref bool value)
        {
            if (value == true && !SecurityHelper.CheckUnmanagedCodePermission())
            {
                value = false;
                throw new SecurityException(SR.Get(SRID.SecurityExceptionForSettingSandboxExternalToTrue));
            }
        }

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandWebBrowserPermission()
        {
            CachedWebBrowserPermission.Demand();
        }

        ///<SecurityNote>
        ///  Critical: Returns a permission object, which can be misused.
        ///</SecurityNote>
        internal static WebBrowserPermission CachedWebBrowserPermission
        {
            [SecurityCritical]
            get
            {
                if (_webBrowserPermission == null)
                {
                    _webBrowserPermission = new WebBrowserPermission(PermissionState.Unrestricted);
                }
                return _webBrowserPermission;
            }
        }
        static WebBrowserPermission _webBrowserPermission;

        ///<summary>
        /// Check whether the call stack has the permissions needed for WebBrowser.
        /// Optimization: false is returned if the AppDomain's permission grant does not include the
        ///   WebBrowserPermission. This preliminary check will defeat an Assert on the callstack, but it
        ///   avoids the SecurityException in PT when the full WB permission is not granted.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CallerAndAppDomainHaveUnrestrictedWebBrowserPermission()
        {
            if (!MS.Internal.SecurityHelper.AppDomainHasPermission(CachedWebBrowserPermission))
                return false;
            try
            {
                SecurityHelper.DemandWebBrowserPermission();
            }
            catch (SecurityException)
            {
                return false;
            }
            return true;
        }

        ///<summary>
        /// Check to see if we have User initiated navigation permission.
        ///</summary>
        /// <returns>true if call stack has UserInitiatedNavigation permission</returns>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CallerHasUserInitiatedNavigationPermission()
        {
            try
            {
                CreateUserInitiatedNavigationPermission();
                _userInitiatedNavigationPermission.Demand();
            }
            catch (SecurityException)
            {
                return false;
            }
            return true;
        }


        ///<summary>
        /// Create a UserInitiatedNavigation permission.
        /// Separate helper exists to make it easy to change what the permission is.
        ///</summary>
        ///<SecurityNote>
        ///  Critical: Returns a permission object, which can be misused.
        ///</SecurityNote>
        [SecurityCritical]
        internal static CodeAccessPermission CreateUserInitiatedNavigationPermission()
        {
            if(_userInitiatedNavigationPermission == null)
            {
                _userInitiatedNavigationPermission = new UserInitiatedNavigationPermission();
            }
            return _userInitiatedNavigationPermission;
        }
        static UserInitiatedNavigationPermission _userInitiatedNavigationPermission = null;

        /// <summary>
        /// Demands for permissions needed to construct the PrintDialog in
        /// full trust mode and/or access full trust properties from dialog.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandPrintDialogPermissions()
        {
            if(_defaultPrintingPermission == null)
            {
                _defaultPrintingPermission = SystemDrawingHelper.NewDefaultPrintingPermission();
            }
            _defaultPrintingPermission.Demand();
        }
        static CodeAccessPermission _defaultPrintingPermission = null;

        ///<summary>
        /// Check to see if we have Reflection permission to create types and access members.
        ///</summary>
        /// <returns>true if call stack has Reflection permission</returns>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CallerHasMemberAccessReflectionPermission()
        {
            try
            {
                if (_reflectionPermission == null)
                {
                    _reflectionPermission = new ReflectionPermission(ReflectionPermissionFlag.MemberAccess);
                }
                _reflectionPermission.Demand();
            }
            catch (SecurityException)
            {
                return false;
            }

            return true;
        }
        static ReflectionPermission _reflectionPermission = null;

#endif

#if PRESENTATION_CORE

        ///<summary>
        /// Check to see if the caller is fully trusted.
        ///</summary>
        /// <returns>true if call stack has unrestricted permission</returns>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool IsFullTrustCaller()
        {
            try
            {
                if (_fullTrustPermissionSet == null)
                {
                    _fullTrustPermissionSet = new PermissionSet(PermissionState.Unrestricted);
                }
                _fullTrustPermissionSet.Demand();
            }
            catch (SecurityException)
            {
                return false;
            }

            return true;
        }
        static PermissionSet _fullTrustPermissionSet = null;

        ///<summary>
        /// Return true if the caller has the correct permission set to get a folder
        /// path.
        ///</summary>
        ///<remarks>
        /// This function exists solely as a an optimazation for the debugger scenario
        ///</remarks>
        /// <SecurityNote>
        ///    Critical: This code extracts the permission set associated with an appdomain by elevating
        ///    TreatAsSafe: The information is not exposed
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CallerHasPermissionWithAppDomainOptimization(params IPermission[] permissionsToCheck)
        {
#if NETFX
            // in case of passing null return true
            if (permissionsToCheck == null)
                return true;
            PermissionSet psToCheck = new PermissionSet(PermissionState.None);
            for ( int i = 0 ; i < permissionsToCheck.Length ; i++ )
            {
                psToCheck.AddPermission(permissionsToCheck[i]);
            }
            PermissionSet permissionSetAppDomain = AppDomain.CurrentDomain.PermissionSet;
            if (psToCheck.IsSubsetOf(permissionSetAppDomain))
            {
                return true;
            }
            return false;
#else
            return true;
#endif
        }

        /// <summary> Enables an efficient check for a specific permisison in the AppDomain's permission grant
        /// without having to catch a SecurityException in the case the permission is not granted.
        /// <summary>
        /// <SecurityNote>
        /// Caveat: This is not a generally valid substitute for doing a full Demand. The main cases not
        /// covered are:
        ///   1) call from PT AppDomain into full-trust one;
        ///   2) captured PT callstack (via ExecutionContext) from another thread or context. Our Dispatcher
        ///     does this.
        ///
        /// Critical: Accesses the Critical AppDomain.PermissionSet, which might contain sensitive information
        ///     such as file paths.
        /// Safe: Does not expose the permission object to the caller.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool AppDomainHasPermission(IPermission permissionToCheck)
        {
#if NETFX
            Invariant.Assert(permissionToCheck != null);
            PermissionSet psToCheck = new PermissionSet(PermissionState.None);
            psToCheck.AddPermission(permissionToCheck);
            return psToCheck.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
#else
            return true;
#endif
        }

        /// <SecurityNote>
        /// Critical: Elevates to extract the AppDomain BaseDirectory and returns it, which is sensitive information.
        /// </SecurityNote>
        [SecurityCritical]
        internal static Uri GetBaseDirectory(AppDomain domain)
        {
            Uri appBase = null;
            new FileIOPermission(PermissionState.Unrestricted).Assert();// BlessedAssert
            try
            {
                appBase = new Uri(domain.BaseDirectory);
            }
            finally
            {
                FileIOPermission.RevertAssert();
            }
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
        /// <SecurityNote>
        ///     Critical: Elevates to call code that extracts
        ///               the deployment code base and is used to make trust decisions
        ///               Assuming we are accessing http content from an https site it will
        ///               throw a demand
        /// </SecurityNote>
        [SecurityCritical]
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
        /// <SecurityNote>
        /// Critical - calls MapUrlToZoneWrapper.
        /// This is safe to be called by untrusted parties; all it does is demand permissions
        /// if a condition for accessing Unc content is not satisfied, but since we want to track
        /// its callers, we are not marking this as SecurityTreatAsSafe.
        /// </SecurityNote>
        [SecurityCritical]
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

         /// <SecurityNote>
         ///   Critical: This code elevates to call MapUrlToZone in the form of a SUC
         /// </SecurityNote>
         [SecurityCritical]
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

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandFilePathDiscoveryWriteRead()
        {
            FileIOPermission permobj = new FileIOPermission(PermissionState.None);
            permobj.AllFiles = FileIOPermissionAccess.Write|FileIOPermissionAccess.Read |FileIOPermissionAccess.PathDiscovery;
            permobj.Demand();
        }

        ///<SecurityNote>
        ///  Critical: This extracts permission set for app domain
        ///</SecurityNote>
        [SecurityCritical]
        internal static PermissionSet ExtractAppDomainPermissionSetMinusSiteOfOrigin()
        {
#if NETFX
            PermissionSet permissionSetAppDomain = AppDomain.CurrentDomain.PermissionSet;
#else
            PermissionSet permissionSetAppDomain = new PermissionSet(PermissionState.Unrestricted);
#endif

            // Ensure we remove the FileIO read permission to site of origin.
            // We choose to use unrestricted here because it does not matter
            // matter which specific variant of Fileio/Web permission we use
            // since we are using an overload to check and remove permission
            // that works on type. There is not a way to remove some
            // part of a permission, although we could remove this and add
            // back the delta if the existing permission set had more than the ones
            // we care about but it is really the path we are targeting here since
            // that is what causes the delta and hence we are removing it all together.
            Uri siteOfOrigin = SiteOfOriginContainer.SiteOfOrigin;
            CodeAccessPermission siteOfOriginReadPermission =  null;
            if (siteOfOrigin.Scheme == Uri.UriSchemeFile)
            {
                siteOfOriginReadPermission = new FileIOPermission(PermissionState.Unrestricted);
            }
            else if (siteOfOrigin.Scheme == Uri.UriSchemeHttp)
            {
                siteOfOriginReadPermission = new WebPermission(PermissionState.Unrestricted);
            }

            if (siteOfOriginReadPermission != null)
            {
                if (permissionSetAppDomain.GetPermission(siteOfOriginReadPermission.GetType()) != null)
                {
                    permissionSetAppDomain.RemovePermission(siteOfOriginReadPermission.GetType());
                    // Failing on a ReadOnlyPermissionSet here? See Dev10.697110.
                }
            }
            return permissionSetAppDomain;
        }

#endif

#if PRESENTATION_CORE
        /// <summary>
        /// determines if the current call stack has the serialization formatter
        /// permission. This is one of the few CLR checks that doesn't have a
        /// bool version - you have to let the check fail and catch the exception.
        ///
        /// Because this is a check *at that point*, you may not cache this value.
        /// </summary>
        /// <returns>true if call stack has the serialization permission</returns>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CallerHasSerializationPermission()
        {
            try
            {
                if(_serializationSecurityPermission == null)
                {
                    _serializationSecurityPermission = new SecurityPermission(SecurityPermissionFlag.SerializationFormatter);
                }
                _serializationSecurityPermission.Demand();
            }
            catch (SecurityException)
            {
                return false;
            }
            return true;
        }
        static SecurityPermission _serializationSecurityPermission = null;

        /// <summary>
        /// determines if the current call stack has the all clipboard
        /// permission. This is one of the few CLR checks that doesn't have a
        /// bool version - you have to let the check fail and catch the exception.
        ///
        /// Because this is a check *at that point*, you may not cache this value.
        /// </summary>
        /// <returns>true if call stack has the all clipboard</returns>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CallerHasAllClipboardPermission()
        {
            try
            {
                SecurityHelper.DemandAllClipboardPermission();
            }
            catch (SecurityException)
            {
                return false;
            }
            return true;
        }

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandAllClipboardPermission()
        {
            if(_uiPermissionAllClipboard == null)
            {
                _uiPermissionAllClipboard = new UIPermission(UIPermissionClipboard.AllClipboard);
            }
            _uiPermissionAllClipboard.Demand();
        }
        static UIPermission _uiPermissionAllClipboard = null;

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandPathDiscovery(string path)
        {
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, path).Demand();
        }

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CheckEnvironmentPermission()
        {
            try
            {
                SecurityHelper.DemandEnvironmentPermission();
            }
            catch (SecurityException)
            {
                return false ;
            }

            return true;
        }

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandEnvironmentPermission()
        {
            if(_unrestrictedEnvironmentPermission == null)
            {
                _unrestrictedEnvironmentPermission = new EnvironmentPermission(PermissionState.Unrestricted);
            }
            _unrestrictedEnvironmentPermission.Demand();
        }
        static EnvironmentPermission _unrestrictedEnvironmentPermission = null;

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandUriDiscoveryPermission(Uri uri)
        {
            CodeAccessPermission permission = CreateUriDiscoveryPermission(uri);
            if (permission != null)
                permission.Demand();
        }

        ///<SecurityNote>
        ///  Critical: Returns a permission object, which can be misused.
        ///</SecurityNote>
        [SecurityCritical]
        internal static CodeAccessPermission CreateUriDiscoveryPermission(Uri uri)
        {
            // explicitly disallow sub-classed Uris to guard against
            // exploits where we "lie" about some of the properties on the Uri.
            // and then later change the value returned
            //      ( e.g. supply a different uri from what checked here)
            if (uri.GetType().IsSubclassOf(typeof(Uri)))
            {
                DemandInfrastructurePermission();
            }

            if (uri.IsFile)
                return new FileIOPermission(FileIOPermissionAccess.PathDiscovery, uri.LocalPath);

            // Add appropriate demands for other Uri types here.
            return null;
        }

        ///<SecurityNote>
        ///  Critical: Returns a permission object, which can be misused.
        ///</SecurityNote>
        [SecurityCritical]
        internal static CodeAccessPermission CreateUriReadPermission(Uri uri)
        {
            // explicitly disallow sub-classed Uris to guard against
            // exploits where we "lie" about some of the properties on the Uri.
            // and then later change the value returned
            //      ( e.g. supply a different uri from what checked here)
            if (uri.GetType().IsSubclassOf(typeof(Uri)))
            {
                DemandInfrastructurePermission();
            }

            if (uri.IsFile)
                return new FileIOPermission(FileIOPermissionAccess.Read, uri.LocalPath);

            // Add appropriate demands for other Uri types here.
            return null;
        }

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandUriReadPermission(Uri uri)
        {
            CodeAccessPermission permission = CreateUriReadPermission(uri);
            if (permission != null)
                permission.Demand();
        }

        /// <summary>
        /// Checks whether the caller has path discovery permission for the input path.
        /// </summary>
        /// <param name="path">Full path to a file or a directory.</param>
        /// <returns>true if the caller has the discovery permission, false otherwise.</returns>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static bool CallerHasPathDiscoveryPermission(string path)
        {
            try
            {
                DemandPathDiscovery(path);
                return true;
            }
            catch (SecurityException)
            {
                return false;
            }
        }

        /// <summary>
        /// The permission set required to use encrypted package envelopes
        /// </summary>
        ///<SecurityNote>
        ///  Critical: Returns a permission object, which can be misused.
        ///</SecurityNote>
        internal static PermissionSet EnvelopePermissionSet
        {
            [SecurityCritical]
            get
            {
                if (_envelopePermissionSet == null)
                {
                    _envelopePermissionSet = CreateEnvelopePermissionSet();
                }
                return _envelopePermissionSet;
            }
        }
        private static PermissionSet _envelopePermissionSet = null;

        /// <summary>
        /// Creates a permission set that includes all permissions necessary to
        /// use EncryptedPackageEnvelope.
        /// </summary>
        /// <returns>The appropriate permission set</returns>
        ///<SecurityNote>
        ///  Critical: Returns a permission object, which can be misused.
        ///</SecurityNote>
        [SecurityCritical]
        private static PermissionSet CreateEnvelopePermissionSet()
        {
            PermissionSet permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(new RightsManagementPermission());
            permissionSet.AddPermission(new CompoundFileIOPermission());

            return permissionSet;
        }

#endif


#if WINDOWS_BASE
        ///<SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        ///</SecurityNote>
        [SecurityCritical]
        internal static void DemandRightsManagementPermission()
        {
            if(_rightsManagementPermission == null)
            {
                _rightsManagementPermission = new RightsManagementPermission();
            }
            _rightsManagementPermission.Demand();
        }
        static RightsManagementPermission _rightsManagementPermission = null;

        ///<SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        ///</SecurityNote>
        [SecurityCritical]
        internal static void DemandCompoundFileIOPermission()
        {
            if(_compoundFileIOPermission == null)
            {
                _compoundFileIOPermission = new CompoundFileIOPermission();
            }
            _compoundFileIOPermission.Demand();
        }
        static CompoundFileIOPermission _compoundFileIOPermission = null;

        ///<SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        ///</SecurityNote>
        [SecurityCritical]
        internal static void DemandPathDiscovery(string path)
        {
            FileIOPermission permobj = new FileIOPermission(PermissionState.None);
            permobj.AddPathList(FileIOPermissionAccess.PathDiscovery, path);
            permobj.Demand();
        }

        /// <SecurityNote>
        /// Note that the XAML reader relies on being able to call RunClassConstructor() on non-public types.
        /// This is considered a security flaw, and a future version of the CLR will likely plug it.
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static void RunClassConstructor(Type t)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
        }

#endif //  WINDOWS_BASE

#if DRT
        /// <SecurityNote>
        /// Critical: This calls into Marshal.SizeOf which has a link demand
        /// TreatAsSafe: Determining size is deemed as a safe operation
        /// </SecurityNote>
        /// <remarks> The LinkDemand on Marshal.SizeOf() was removed in v4. </remarks>
        [SecuritySafeCritical]
        internal static int SizeOf(Type t)
        {
            return Marshal.SizeOf(t);
        }
#endif

#if DRT
        /// <SecurityNote>
        /// Critical: This calls into Marshal.SizeOf which has a link demand
        /// TreatAsSafe: Determining size is deemed as a safe operation
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static int SizeOf(Object o)
        {
            return Marshal.SizeOf(o);
        }
#endif

#if WINDOWS_BASE || PRESENTATION_CORE || PRESENTATIONFRAMEWORK
        /// <SecurityNote>
        /// Critical: This calls into Marshal.GetExceptionForHR which is critical
        ///           it populates the exception object from data stored in a per thread IErrorInfo
        ///           the IErrorInfo may have security sensitive information like file paths stored in it
        /// TreatAsSafe: Uses overload of GetExceptionForHR that omits IErrorInfo information from exception message
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static Exception GetExceptionForHR(int hr)
        {
            return Marshal.GetExceptionForHR(hr, new IntPtr(-1));
        }
#endif

#if WINDOWS_BASE || PRESENTATION_CORE
        /// <SecurityNote>
        /// Critical: This calls into Marshal.ThrowExceptionForHR which is critical because 
        ///           it populates the exception object from data stored in a per thread IErrorInfo
        ///           the IErrorInfo may have security sensitive information like file paths stored in it
        /// TreatAsSafe: Uses overload of ThrowExceptionForHR that omits IErrorInfo information from exception message
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal static void ThrowExceptionForHR(int hr)
        {
            Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
        }
        
        /// <SecurityNote>
        /// Critical: This calls into Marshal.GetHRForException which is critical
        ///           because it fills a per thread IErrorInfo with data in the Exception
        ///           the Exception may have security sensitive data like file paths stored in it
        /// TreatAsSafe: Clears the per thread IErrorInfo by calling GetHRForException a second time 
        ///              with an exception object with no security sensitive information
        /// </SecurityNote>
        [SecuritySafeCritical]
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

#if PRESENTATION_CORE
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandRegistryPermission()
        {
            if(_unrestrictedRegistryPermission == null)
            {
                _unrestrictedRegistryPermission = new RegistryPermission(PermissionState.Unrestricted);
            }
            _unrestrictedRegistryPermission.Demand();
        }
        static RegistryPermission _unrestrictedRegistryPermission = null;
#endif // PRESENTATION_CORE

#if !PBTCOMPILER
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandUIWindowPermission()
        {
            if(_allWindowsUIPermission == null)
            {
                _allWindowsUIPermission = new UIPermission(UIPermissionWindow.AllWindows);
            }
            _allWindowsUIPermission.Demand();
        }
        static UIPermission _allWindowsUIPermission = null;
#endif
#if PRESENTATION_CORE
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandInfrastructurePermission()
        {
            if(_infrastructurePermission == null)
            {
                _infrastructurePermission = new SecurityPermission( SecurityPermissionFlag.Infrastructure );
            }
            _infrastructurePermission.Demand();
        }
        static SecurityPermission _infrastructurePermission = null;

#endif

#if PRESENTATION_CORE || REACHFRAMEWORK

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandMediaPermission(MediaPermissionAudio audioPermissionToDemand,
                                                   MediaPermissionVideo videoPermissionToDemand,
                                                   MediaPermissionImage imagePermissionToDemand)
        {
            // Demand the appropriate permission
           (new MediaPermission(audioPermissionToDemand,
                                videoPermissionToDemand,
                                imagePermissionToDemand )).Demand();
        }


        ///<summary>
        /// Check whether the call stack has the permissions needed for safe media.
        ///
        /// </summary>
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
#if REACHFRAMEWORK        
        [SecurityCritical]
#else        
        [SecuritySafeCritical]
#endif        
        internal static bool CallerHasMediaPermission(MediaPermissionAudio audioPermissionToDemand,
                                                      MediaPermissionVideo videoPermissionToDemand,
                                                      MediaPermissionImage imagePermissionToDemand)
        {
            try
            {
                (new MediaPermission(audioPermissionToDemand,videoPermissionToDemand,imagePermissionToDemand)).Demand();
                return true;
            }
            catch(SecurityException)
            {
                    return false;
            }
        }
#endif



        // don't include this in the compiler - avoid compiler changes when we can.
#if !PBTCOMPILER
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
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
        internal static bool AppDomainGrantedUnrestrictedUIPermission
        {
            [SecurityCritical]
            get
            {
                if(!_appDomainGrantedUnrestrictedUIPermission.HasValue)
                {
                    _appDomainGrantedUnrestrictedUIPermission = AppDomainHasPermission(new UIPermission(PermissionState.Unrestricted));
                }

                return _appDomainGrantedUnrestrictedUIPermission.Value;
            }
        }
        [SecurityCritical]
        private static bool? _appDomainGrantedUnrestrictedUIPermission;

#endif

#if PRESENTATION_CORE
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandFileIOReadPermission(string fileName)
        {
            new FileIOPermission(FileIOPermissionAccess.Read, fileName).Demand();
        }
#endif

#if NEVER
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
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

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
        internal static void DemandUnrestrictedFileIOPermission()
        {
            if(_unrestrictedFileIOPermission == null)
            {
                _unrestrictedFileIOPermission = new FileIOPermission(PermissionState.Unrestricted);
            }
            _unrestrictedFileIOPermission.Demand();
        }
        static FileIOPermission _unrestrictedFileIOPermission = null;

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
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
        /// <SecurityNote>
        ///     Critical - Elevates for unmanaged code permissions to display a messagebox (internally, it calls
        ///                MessageBox WIN32 API).
        ///     TreatAsSafe - The MessageBox API takes a static strings and known enums as input.  There is nothing
        ///                   unsafe with the dialog and it is not spoofable from Avalon as it is a Win32 window.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
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
        /// <SecurityNote>
        ///     Critical - Elevates for unmanaged code permissions to display a messagebox (internally, it calls
        ///                MessageBox WIN32 API).
        ///     TreatAsSafe - The MessageBox API takes a static strings and known enums as input.  There is nothing
        ///                   unsafe with the dialog and it is not spoofable from Avalon as it is a Win32 window.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
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
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
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
        ///<SecurityNote>
        ///  Critical: Returns a permission object, which can be misused.
        ///</SecurityNote>
        [SecurityCritical]
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
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// Safe     - The method denies the caller access to the full exception object.
        /// </SecurityNote>
#if REACHFRAMEWORK        
        [SecurityCritical]
#else        
        [SecuritySafeCritical]
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

        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        [SecurityCritical]
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
        /// <SecurityNote>
        /// Critical - Exceptions raised by a demand may contain security sensitive information that should not be passed to transparent callers
        /// </SecurityNote>
        /// <summary>
        /// By default none of the plug-in serializer code must succeed for partially trusted callers
        /// </summary>
        [SecurityCritical]
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

        /// <SecurityNote>
        /// This method has security implications: Mime\ContentTypes\Uri schemes types are served in a case-insensitive fashion; they MUST be compared that way
        /// </SecurityNote>
        internal static bool AreStringTypesEqual(string m1, string m2)
        {
            return (String.Compare(m1, m2, StringComparison.OrdinalIgnoreCase) == 0);
        }

#endif //PRESENTATION_CORE || PRESENTATIONFRAMEWORK || WINDOWS_BASE


#if WINDOWS_BASE
        ///
        /// Read and return a registry value.
        ///<SecurityNote>
        /// Critical - Asserts registry permission on the caller-provided key.
        ///</SecurityNote>
       [SecurityCritical]
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

