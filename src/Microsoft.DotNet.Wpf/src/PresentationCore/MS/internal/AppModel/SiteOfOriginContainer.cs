// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
// SiteOfOriginContainer is an implementation of the abstract Package class. 
// It contains nontrivial overrides for GetPartCore and Exists.
// Many of the methods on Package are not applicable to loading application 
// resources, so the SiteOfOriginContainer implementations of these methods throw 
// the NotSupportedException.
// 

using System;
using System.IO.Packaging;
using System.IO;
using System.Collections.Generic;
using System.Windows.Resources;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.Windows;
using System.Windows.Navigation;
#if CLICKONCE
using System.Deployment.Application;
#endif
using System.Security;
using System.Security.Permissions;
using MS.Internal.PresentationCore;

namespace MS.Internal.AppModel
{
    /// <summary>
    /// SiteOfOriginContainer is an implementation of the abstract Package class. 
    /// It contains nontrivial overrides for GetPartCore and Exists.
    /// Many of the methods on Package are not applicable to loading files 
    /// so the SiteOfOriginContainer implementations of these methods throw 
    /// the NotSupportedException.
    /// </summary>
    internal class SiteOfOriginContainer : System.IO.Packaging.Package
    {
        //------------------------------------------------------
        //
        //  Static Methods
        //
        //------------------------------------------------------

        #region Static Methods

        internal static Uri SiteOfOrigin
        {
            [FriendAccessAllowed]
            get
            {
                Uri siteOfOrigin = SiteOfOriginForClickOnceApp;
                if (siteOfOrigin == null)
                {
                    // Calling FixFileUri because BaseDirectory will be a c:\\ style path
                    siteOfOrigin = BaseUriHelper.FixFileUri(new Uri(System.AppDomain.CurrentDomain.BaseDirectory));
                }
#if DEBUG
            if (_traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + 
                        ": SiteOfOriginContainer: returning site of origin " + siteOfOrigin);
#endif

                return siteOfOrigin;
            }
        }

        // we separated this from the rest of the code because this code is used for media permission
        // tests in partial trust but we want to do this without hitting the code path for regular exe's
        // as in the code above. This will get hit for click once apps, xbaps, xaml and xps
        ///<SecurityNote>
        ///     Critical: sets critical data _siteOfOriginForClickOnceApp.
        ///     TreatAsSafe: the source we set it to is trusted. It is also safe to get the app's site of origin.
        ///</SecurityNote> 
        internal static Uri SiteOfOriginForClickOnceApp
        {
            [SecurityCritical, SecurityTreatAsSafe, FriendAccessAllowed]
            get
            {
                // The ClickOnce API, ApplicationDeployment.IsNetworkDeployed, determines whether the app is network-deployed
                // by getting the ApplicationDeployment.CurrentDeployment property and catch the exception it can throw.
                // The exception is a first chance exception and caught, but it often confuses developers,
                // and can also have a perf impact. So we change to cache the value of SiteofOrigin in Dev10 to avoid the 
                // exception being thrown too many times.
                // An alternative is to cache the value of ApplicationDeployment.IsNetworkDeployed.
                if (_siteOfOriginForClickOnceApp == null)
                {
#if CLICKONCE
                    if (_browserSource.Value != null)
                    {
                        _siteOfOriginForClickOnceApp = new SecurityCriticalDataForSet<Uri>(_browserSource.Value);
                    }
                    else if (ApplicationDeployment.IsNetworkDeployed)
                    {
                        _siteOfOriginForClickOnceApp = new SecurityCriticalDataForSet<Uri>(GetDeploymentUri());
                    }
                    else
                    {
                        _siteOfOriginForClickOnceApp = new SecurityCriticalDataForSet<Uri>(null);
                    }
#else
                    _siteOfOriginForClickOnceApp = new SecurityCriticalDataForSet<Uri>(null);
#endif

                }

                Invariant.Assert(_siteOfOriginForClickOnceApp != null);

                return _siteOfOriginForClickOnceApp.Value.Value;
            }
        }
       
        /// <securitynote>
        /// Critical    - Whatever is set here will be treated as site of origin.
        /// </securitynote>
        internal static Uri BrowserSource
        {
            get
            {
                return _browserSource.Value;
            }
            [SecurityCritical, FriendAccessAllowed]
            set
            {    
               _browserSource.Value = value; 
            }
        }
   
        #endregion

        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        internal SiteOfOriginContainer() : base(FileAccess.Read)
        {         
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        // None  

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------        

        #region Public Methods

        /// <remarks>
        /// If this were to be implemented for http site of origin, 
        /// it will require a server round trip.
        /// </remarks>
        /// <param name="uri"></param>
        /// <returns></returns>
        public override bool PartExists(Uri uri)
        {
            return true;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        ///<SecurityNote>
        ///     Critical: sets BooleanSwitch.Enabled which LinkDemands
        ///     TreatAsSafe: ok to enable tracing
        ///</SecurityNote> 
        internal static bool TraceSwitchEnabled
        {
            get
            {
                return _traceSwitch.Enabled;
            }
            [SecurityCritical, SecurityTreatAsSafe]
            set
            {
                _traceSwitch.Enabled = value;
            }
        }

        internal static System.Diagnostics.BooleanSwitch _traceSwitch = 
            new System.Diagnostics.BooleanSwitch("SiteOfOrigin", "SiteOfOriginContainer and SiteOfOriginPart trace messages");

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Protected Constructors
        //
        //------------------------------------------------------
        // None

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// This method creates a SiteOfOriginPart which will create a WebRequest
        /// to access files at the site of origin.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected override PackagePart GetPartCore(Uri uri)
        {
#if DEBUG
            if (_traceSwitch.Enabled)
                System.Diagnostics.Trace.TraceInformation(
                        DateTime.Now.ToLongTimeString() + " " + DateTime.Now.Millisecond + " " +
                        System.Threading.Thread.CurrentThread.ManagedThreadId + 
                        ": SiteOfOriginContainer: Creating SiteOfOriginPart for Uri " + uri);
#endif
            return new SiteOfOriginPart(this, uri);
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

#if CLICKONCE
        /// <securitynote>
        /// Critical    - Performs an elevation to access ApplicationIdentity
        /// TreatAsSafe - Returns the Uri the .application/.xapp was launched from if the application was network deployed.
        ///               This will not work for a standalone application due to the invariant assert.  This information is
        ///               not considered critical because an application is allowed to know where it was deployed from just 
        ///               not the local path it is running from.  
        ///               This information is also typically available from ApplicationDeployment.CurrentDeployment.ActivationUri 
        ///               or ApplicationDeployment.CurrentDeployment.UpdateLocation but those properties are null for applications
        ///               that do not take Uri parameters and do not receive updates from the web.  Neither of those properties 
        ///               requires an assert to access and are available in partial trust.  We are not using them because the need 
        ///               for a site of origin is not dependent on update or uri parameters.
        /// </securitynote>
        [SecurityCritical, SecurityTreatAsSafe]
        private static Uri GetDeploymentUri()
        {
            Invariant.Assert(ApplicationDeployment.IsNetworkDeployed);
            AppDomain currentDomain = AppDomain.CurrentDomain;
            ApplicationIdentity ident = null;
            string codeBase = null;

            SecurityPermission p1 = new SecurityPermission(SecurityPermissionFlag.ControlDomainPolicy);
            p1.Assert();
            try
            {
                ident = currentDomain.ApplicationIdentity; // ControlDomainPolicy
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
            
            SecurityPermission p2 = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
            p2.Assert();
            try
            {
                codeBase = ident.CodeBase; // Unmanaged Code permission
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }

            return new Uri(new Uri(codeBase), new Uri(".", UriKind.Relative));
        }
#endif
    
        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        private static SecurityCriticalDataForSet<Uri> _browserSource;
        private static SecurityCriticalDataForSet<Uri>? _siteOfOriginForClickOnceApp;

        #endregion Private Members

        //------------------------------------------------------
        //
        //  Uninteresting (but required) overrides
        //
        //------------------------------------------------------
        #region Uninteresting (but required) overrides

        protected override PackagePart CreatePartCore(Uri uri, string contentType, CompressionOption compressionOption)
        {
            return null;
        }

        protected override void DeletePartCore(Uri uri)
        {
            throw new NotSupportedException();
        }

        protected override PackagePart[] GetPartsCore()
        {
            throw new NotSupportedException();
        }

        protected override void FlushCore()
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
