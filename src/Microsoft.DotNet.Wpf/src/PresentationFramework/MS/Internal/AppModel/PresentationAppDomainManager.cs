// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
// Description:
//      Implements a custom AppDomainManager.
//
//---------------------------------------------------------------------------

using System;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Runtime.Hosting;
using System.Text;
using MS.Win32;
using MS.Internal;
using MS.Internal.AppModel;
using MS.Internal.Utility;
using MS.Utility;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Windows.Interop
{
    internal class PresentationHostSecurityManager : HostSecurityManager
    {
        internal static IntPtr ElevationPromptOwnerWindow;

        internal PresentationHostSecurityManager()
        {
        }

        public override ApplicationTrust DetermineApplicationTrust(Evidence applicationEvidence, Evidence activatorEvidence, TrustManagerContext context)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_DetermineApplicationTrustStart);

            ApplicationTrust trust;
            Uri activationUri = GetUriFromActivationData(0);
            bool isDebug = PresentationAppDomainManager.IsDebug ? true : GetBoolFromActivationData(1);

            BrowserInteropHelper.SetBrowserHosted(true);

            if (isDebug)
            {
                context.IgnorePersistedDecision = true;
                context.Persist = false;
                context.KeepAlive = false;
                context.NoPrompt = true;
                trust = base.DetermineApplicationTrust(applicationEvidence, activatorEvidence, context);
            }
            else
            {
                // Elevation prompt for permissions beyond the default for the security zone is allowed only
                // in the Intranet and Trusted Sites zones (v4).
                Zone hostEvidence = applicationEvidence.GetHostEvidence<Zone>();
                context.NoPrompt = !(hostEvidence.SecurityZone == SecurityZone.Intranet || hostEvidence.SecurityZone == SecurityZone.Trusted);
                /*
                Now we need to convince the ClickOnce elevation prompt to use the browser's top-level window as
                the owner in order to block the browser's UI (and our Cancel button) and ensure the prompt 
                stays on top. This is not easy.
                 * The prompt dialog is created without an explicit owner, on its own thread.
                 * There are layers of ClickOnce and pure security code before the UI is invoked (that's 
                   TrustManagerPromptUIThread in System.Windows.Forms.dll). So, passing the owner window handle
                   would require some awkward plumbing.
                 
                Since the dialog is shown on a separate thread, intercepting its creation or display is 
                complicated. An EVENT_OBJECT_CREATE hook can do it. But there is a cascade of thread 
                synchonization/access and window state issues if trying to set the owner on the fly.
                 
                The cleanest solution ended up resorting to Detours. When not given an owner window, 
                SWF.Form.ShowDialog() uses the active window as owner. Since the call to GetActiveWindow() 
                occurs on a new thread, where there are no other windows, we couldn't just pre-set the owner
                as the active window. So, we intercept the GetActiveWindow() call and return the browser's
                top-level window. From that point on, everything in the WinForms dialog works as if the owner
                was explicitly given. (And owner from a different thread or process is fully supported.)
                 
                This condition is an optimization.
                DetermineApplicationTrust() is called up to 3 times: twice in the default AppDomain and once
                in the new one. Empirically, the elevation prompt is shown during the first call. 
                */
                bool forceOwner = !context.NoPrompt && ElevationPromptOwnerWindow != IntPtr.Zero;
                if(forceOwner)
                {
                    // The native code passes the DocObject top window, not the browser's top-level window, 
                    // but we need exactly the top-level one.
                    IntPtr ownerWindow = UnsafeNativeMethods.GetAncestor(
                        new HandleRef(null, ElevationPromptOwnerWindow), NativeMethods.GA_ROOT);
                    SetFakeActiveWindow(ownerWindow);
                    ElevationPromptOwnerWindow = IntPtr.Zero; // to prevent further prompting
                }
                try
                {
                    trust = base.DetermineApplicationTrust(applicationEvidence, activatorEvidence, context);
                }
                finally
                {
                    if (forceOwner)
                    {
                        SetFakeActiveWindow(new IntPtr());
                    }
                }
            }

            // Modify the permission grant set if necessary.
            if (trust != null)
            {
                PermissionSet permissions = trust.DefaultGrantSet.PermissionSet;

                if (isDebug)
                {
                    Uri debugSecurityZoneURL = GetUriFromActivationData(2);
                    if (debugSecurityZoneURL != null)
                    {
                        permissions = AddPermissionForUri(permissions, debugSecurityZoneURL);
                    }
                }

                // CLR v4 breaking change: In some activation scenarios we get a ReadOnlyPermissionSet. 
                // This is a problem because:
                //   - Code may expect AppDomain.PermissionSet (or the old AppDomain.ApplicationTrust.
                //      DefaultGrantSet.PermissionSet) to return a mutable PermissionSet.
                //   - The ReadOnlyPermissionSet may have v2 and v3 assembly references--they are not 'unified'
                //      to the current framework version. This might confuse code doing more involved permission
                //      set comparisons or calculations.
                // Workaround is to copy the ROPS to a regular one.
                if (permissions is ReadOnlyPermissionSet)
                {
                    permissions = new PermissionSet(permissions); 
                }

                trust.DefaultGrantSet.PermissionSet = permissions;
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_DetermineApplicationTrustEnd);

            return trust;
        }

        [DllImport(ExternDll.PresentationHostDll)]
        static extern void SetFakeActiveWindow(IntPtr hwnd);

        internal static PermissionSet AddPermissionForUri(PermissionSet originalPermSet, Uri srcUri)
        {
            PermissionSet newPermSet = originalPermSet;
            if (srcUri != null)
            {
                Evidence evidence = new Evidence();
                evidence.AddHost(new Url(BindUriHelper.UriToString(srcUri))); // important: the parameter must be a UrL object not a UrI object
                IMembershipCondition membership = new UrlMembershipCondition(BindUriHelper.UriToString(srcUri));
                CodeGroup group = (srcUri.IsFile) ?
                    (CodeGroup)new FileCodeGroup(membership, FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery)
                    :(CodeGroup)new NetCodeGroup(membership);
                PolicyStatement policy = group.Resolve(evidence);
                if (!policy.PermissionSet.IsEmpty())
                {
                    newPermSet = originalPermSet.Union(policy.PermissionSet);
                }
            }
            return newPermSet;
        }

        private bool GetBoolFromActivationData(int index)
        {
            bool flag = false; // default

            if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null &&
                AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData.Length > index)
            {
                if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[index] == true.ToString())
                {
                    flag = true;
                }
            }

            return flag;
        }

        private Uri GetUriFromActivationData(int index)
        {
            Uri uri = null;

            if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null &&
                AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData.Length > index)
            {
                if (!string.IsNullOrEmpty(AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[index]))
                {
                    uri = new UriBuilder(AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[index]).Uri;
                }
            }

            return uri;
        }
    }

    // This is the custom ApplicationActivator that will be returned by
    // the PresentationAppDomainManager.ApplicationActivator property.
    // CreateInstance will be called twice: the first time to create
    // the new AppDomain, and the second time to create the app inside
    // of the new AppDomain.
    internal class PresentationApplicationActivator : System.Runtime.Hosting.ApplicationActivator
    {
        public override ObjectHandle CreateInstance(ActivationContext actCtx)
        {
            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose))
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WpfHost_ApplicationActivatorCreateInstanceStart,
                        EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose,
                        PresentationAppDomainManager.ActivationUri != null ? PresentationAppDomainManager.ActivationUri.ToString() : string.Empty);
            }

            ObjectHandle oh;
            if (PresentationAppDomainManager.ActivationUri != null)
            {
                oh = base.CreateInstance(
                    actCtx,
                    new string[] {
                    BindUriHelper.UriToString(PresentationAppDomainManager.ActivationUri),
                    PresentationAppDomainManager.IsDebug.ToString(),
                    (PresentationAppDomainManager.DebugSecurityZoneURL == null?
                        string.Empty
                        : PresentationAppDomainManager.DebugSecurityZoneURL.ToString())});
            }
            else
            {
                oh = base.CreateInstance(actCtx);
            }
            bool returnAppDomain = false;

            try
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert(); // BlessedAssert:
                if (AppDomain.CurrentDomain.ActivationContext != null &&
                    AppDomain.CurrentDomain.ActivationContext.Identity.ToString().Equals(actCtx.Identity.ToString()))
                {
                    returnAppDomain = true;
                }
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_ApplicationActivatorCreateInstanceEnd);

            if (returnAppDomain)
            {
                // This is the new AppDomain. What we return here becomes the return value of
                // InPlaceHostingManager.Execute().
                return new ObjectHandle(AppDomain.CurrentDomain);
            }
            else
            {
                return oh;
            }
        }
    }

    // This is a custom AppDomainManager class we're using.  We need to set the
    // assembly name and class name in the environment for CLR to use it.  We
    // need to use this to detect new AppDomain creation.
    internal class PresentationAppDomainManager : AppDomainManager
    {
        static PresentationAppDomainManager()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_AppDomainManagerCctor);
        }

        public PresentationAppDomainManager()
        {
        }

        public override ApplicationActivator ApplicationActivator
        {
            get
            {
                if (_appActivator == null)
                    _appActivator = new PresentationApplicationActivator();
                return _appActivator;
            }
        }

        public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
        {
            //Hookup the assembly load event
            _assemblyFilter = new AssemblyFilter();
            AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(_assemblyFilter.FilterCallback);
        }

        public override HostSecurityManager HostSecurityManager
        {
            get
            {
                if (_hostsecuritymanager == null)
                {
                    _hostsecuritymanager = new PresentationHostSecurityManager();
                }
                return _hostsecuritymanager;
            }
        }

        // Creates ApplicationProxyInternal.  Creating it from Default domain will
        // cause a stack walk for ReflectionPermission which will fail for partial
        // trust apps.
        internal ApplicationProxyInternal CreateApplicationProxyInternal()
        {
            return new ApplicationProxyInternal();
        }

        internal static AppDomain NewAppDomain
        {
            get { return _newAppDomain; }
            set { _newAppDomain = value; }
        }

        internal static bool SaveAppDomain
        {
            get { return _saveAppDomain; }
            set
            {
                _saveAppDomain = value;

                // Allow garbage collection to happen.
                _newAppDomain = null;
            }
        }

        internal static Uri ActivationUri
        {
            get { return _activationUri; }
            set { _activationUri = value; }
        }

        internal static Uri DebugSecurityZoneURL
        {
            get { return _debugSecurityZoneURL; }
            set { _debugSecurityZoneURL = value; }
        }

        internal static bool IsDebug
        {
            get { return _isdebug; }
            set { _isdebug = value; }
        }

        private static bool _isdebug = false;
        private ApplicationActivator _appActivator = null;

        private HostSecurityManager _hostsecuritymanager = null;

        private static AppDomain _newAppDomain;
        private static bool _saveAppDomain;
        private static Uri _activationUri;
        private static Uri _debugSecurityZoneURL;

        private AssemblyFilter _assemblyFilter;
    }
}
