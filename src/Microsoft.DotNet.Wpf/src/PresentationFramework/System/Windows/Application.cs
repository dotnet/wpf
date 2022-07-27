// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//              The Application object is a global object that the Avalon platform
//              uses to identify, reference, and communicate with an Avalon application.
//
//              The application object also presents a consistent logical view of an application
//              to an Avalon developer.
//              An Avalon developer uses the application object to listen and respond to application-wide
//              events (like startup, shutdown, and navigation events),
//              to define global properties and maintain state across multiple pages of markup.
//


//In order to avoid generating warnings about unknown message numbers and unknown pragmas
//when compiling your C# source code with the actual C# compiler, you need to disable
//warnings 1634 and 1691. (From PreSharp Documentation)
#pragma warning disable 1634, 1691

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Resources;
using System.Threading;

using System.IO.Packaging;
using System.Windows.Threading;
using System.Windows.Navigation;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Resources;
using System.Windows.Markup;
using System.Net;
using System.Text;

using MS.Internal;
using MS.Internal.AppModel;
using MS.Internal.IO.Packaging;
using MS.Internal.Interop;
using MS.Internal.Navigation;
using MS.Internal.Telemetry;
using MS.Internal.Utility;
using MS.Internal.Resources;
using MS.Utility;
using MS.Win32;
using Microsoft.Win32;
using MS.Internal.Telemetry.PresentationFramework;

using PackUriHelper = System.IO.Packaging.PackUriHelper;

namespace System.Windows
{
    /// <summary>
    /// Delegate for Startup Event
    /// </summary>
    public delegate void StartupEventHandler(Object sender, StartupEventArgs e);

    /// <summary>
    /// Delegate for the Exit event.
    /// </summary>
    public delegate void ExitEventHandler(Object sender, ExitEventArgs e);

    /// <summary>
    /// Delegate for SessionEnding event
    /// </summary>
    public delegate void SessionEndingCancelEventHandler(Object sender, SessionEndingCancelEventArgs e);

    #region Application Class

    /// <summary>
    /// Application base class
    /// </summary>
    public class Application : DispatcherObject, IHaveResources, IQueryAmbient
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// The static constructor calls ApplicationInit
        /// </summary>
        static Application()
        {
            ApplicationInit();
        }

        /// <summary>
        ///     Application constructor
        /// </summary>
        public Application()
        {
#if DEBUG_CLR_MEM
            if (CLRProfilerControl.ProcessIsUnderCLRProfiler &&
               (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
            {
                CLRProfilerControl.CLRLogWriteLine("Application_Ctor");
            }
#endif // DEBUG_CLR_MEM

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientAppCtor);

            lock(_globalLock)
            {
                // set the default statics
                // DO NOT move this from the begining of this constructor
                if (_appCreatedInThisAppDomain == false)
                {
                    Debug.Assert(_appInstance == null, "_appInstance must be null here.");
                    _appInstance = this;
                    IsShuttingDown    = false;
                    _appCreatedInThisAppDomain = true;
                }
                else
                {
                    //lock will be released, so no worries about throwing an exception inside the lock
                    throw new InvalidOperationException(SR.Get(SRID.MultiSingleton));
                }
            }


            //
            // (Application not shutting down when calling
            // Application.Current.Shutdown())
            //
            // post item to do startup work
            // posting it here so that this is the first item in the queue. Devs
            // could post items before calling run and then those will be serviced
            // before if we don't post this one here.
            //
            // Also, doing startup (firing OnStartup etc.) once our dispatcher
            // is run ensures that we run before any external code is run in the
            // application's Dispatcher.
            Dispatcher.BeginInvoke(
                DispatcherPriority.Send,
                (DispatcherOperationCallback) delegate(object unused)
                {
                    // Shutdown may be started before the Dispatcher gets to this callback.
                    // This can happen in browser-hosted applications.
                    if (IsShuttingDown)
                        return null;

                    // Event handler exception continuality: if exception occurs in Startup event handler,
                    // our state would not be corrupted because it is fired by posting the item in the queue.
                    // Please check Event handler exception continuality if the logic changes.
                    StartupEventArgs e = new StartupEventArgs();
                    OnStartup(e);

                    // PerformDefaultAction is used to cancel the default navigation for the case
                    // when the app is being loaded as a result of a history navigation.  In such
                    // a case, we don't want to show the startupUri page, rather we show the last
                    // page of the app that we in use before navigating away from it.
                    if (e.PerformDefaultAction)
                    {
                        DoStartup();
                    }
                    return null;
                },
                null);
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        ///<summary>
        ///     Run is called to start an application.
        ///
        ///     Typically a developer will do some setting of properties/attaching to events after instantiating an application object,
        ///     and then call Run() to start the application.
        ///</summary>
        ///<remarks>
        ///     Once run has been called - an application's OnStartup override and Startup event is called
        ///     immediately afterwards.
        ///</remarks>
        /// <returns>ExitCode of the application</returns>
        public int Run()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientAppRun);
            return this.Run(null);
        }

        ///<summary>
        ///     Run is called to start an application.
        ///
        ///     Typically a developer will do some setting of properties/attaching to events after instantiating an application object,
        ///     and then call Run() to start the application.
        ///     Any arguments passed in as a string array will be added to the command Line property of the StartupEventArgs property.
        ///     The exit code returned by Run will typically be returned to the OS as a return value.
        ///</summary>
        ///<remarks>
        ///     Once run has been called - an application's OnStartup override and Startup event is called
        ///     immediately afterwards.
        ///</remarks>
        /// <param name="window">Window that will be added to the Windows property and made the MainWindow of the Applcation.
        /// The passed Window must be created on the same thread as the Application object.  Furthermore, this Window is
        /// shown once the Application is run.</param>
        /// <returns>ExitCode of the application</returns>
        public int Run(Window window)
        {
            VerifyAccess();
            return RunInternal(window);
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        internal object GetService(Type serviceType)
        {
            // this is called only from OleCmdHelper and it gets
            // service for IBrowserCallbackServices which is internal.
            // This call is made on the App thread.
            //
            VerifyAccess();
            object service = null;

            if (ServiceProvider != null)
            {
                service = ServiceProvider.GetService(serviceType);
            }
            return service;
        }

        /// <summary>
        ///     Shutdown is called to programmatically shutdown an application.
        ///
        ///     Once shutdown() is called, the application gets called with the
        ///     OnShutdown method to raise the Shutdown event.
        ///     Requires SecurityPermission for unmanaged code
        /// </summary>
        /// <remarks>
        ///     Requires UIPermission with AllWindows access
        /// </remarks>
        public void Shutdown()
        {
            Shutdown(0);
        }

        /// <summary>
        ///     Shutdown is called to programmatically shutdown an application.
        ///
        ///     Once shutdown() is called, the application gets called with the
        ///     OnShutdown method to raise the Shutdown event.
        ///     The exitCode parameter passed in at Shutdown will be returned as a
        ///     return parameter on the run() method, so it can be passed back to the OS.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        /// <param name="exitCode">returned to the Application.Run() method. Typically this will be returned to the OS</param>
        public void Shutdown(int exitCode)
        {
            CriticalShutdown(exitCode);
        }
        internal void CriticalShutdown(int exitCode)
        {
            VerifyAccess();
            //Already called once??
            if (IsShuttingDown == true)
            {
                return;
            }

            ControlsTraceLogger.LogUsedControlsDetails();

            SetExitCode(exitCode);
            IsShuttingDown = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(ShutdownCallback), null);
}

        /// <summary>
        ///     Searches for a resource with the passed resourceKey and returns it
        /// </summary>
        /// <remarks>
        ///     If the sources is not found on the App, SystemResources are searched.
        /// </remarks>
        /// <param name="resourceKey">Name of the resource</param>
        /// <returns>The found resource.</returns>
        /// <exception cref="ResourceReferenceKeyNotFoundException"> if the key is not found.</exception>
        public object FindResource(object resourceKey)
        {
            ResourceDictionary resources = _resources;
            object resource = null;

            if (resources != null)
            {
                resource = resources[resourceKey];
            }

            if (resource == DependencyProperty.UnsetValue  || resource == null)
            {
                // This is the top of the tree, try the system resource collection
                // Safe from multithreading issues, SystemResources uses the SyncRoot lock to access the resource
                resource = SystemResources.FindResourceInternal(resourceKey);
            }

            if (resource == null)
            {
                Helper.ResourceFailureThrow(resourceKey);
            }

            return resource;
        }

        /// <summary>
        ///     Searches for a resource with the passed resourceKey and returns it
        /// </summary>
        /// <remarks>
        ///     If the sources is not found on the App, SystemResources are searched.
        /// </remarks>
        /// <param name="resourceKey">Name of the resource</param>
        /// <returns>The found resource.  Null if not found.</returns>
        public object TryFindResource(object resourceKey)
        {
            ResourceDictionary resources = _resources;
            object resource = null;

            if (resources != null)
            {
                resource = resources[resourceKey];
            }

            if (resource == DependencyProperty.UnsetValue  || resource == null)
            {
                // This is the top of the tree, try the system resource collection
                // Safe from multithreading issues, SystemResources uses the SyncRoot lock to access the resource
                resource = SystemResources.FindResourceInternal(resourceKey);
            }
            return resource;
        }

        //
        // Internal routine only look up in application resources
        //
        internal object FindResourceInternal(object resourceKey)
        {
            // Call Forwarded
            return FindResourceInternal(resourceKey, false /*allowDeferredResourceReference*/, false /*mustReturnDeferredResourceReference*/);
        }

        internal object FindResourceInternal(object resourceKey, bool allowDeferredResourceReference, bool mustReturnDeferredResourceReference)
        {
            ResourceDictionary resources = _resources;

            if (resources == null)
            {
                return null;
            }
            else
            {
                bool canCache;
                return resources.FetchResource(resourceKey, allowDeferredResourceReference, mustReturnDeferredResourceReference, out canCache);
            }
        }

        /// <summary>
        /// Create logic tree from given resource Locator, and associate this
        /// tree with the given component.
        /// </summary>
        /// <param name="component">Root Element</param>
        /// <param name="resourceLocator">Resource Locator</param>
        public static void LoadComponent(Object component, Uri resourceLocator)
        {
            if (component == null)
                throw new ArgumentNullException("component");

            if (resourceLocator == null)
                throw new ArgumentNullException("resourceLocator");

            if (resourceLocator.OriginalString == null)
                throw new ArgumentException(SR.Get(SRID.ArgumentPropertyMustNotBeNull,"resourceLocator", "OriginalString"));

            if (resourceLocator.IsAbsoluteUri == true)
                throw new ArgumentException(SR.Get(SRID.AbsoluteUriNotAllowed));

            // Passed a relative Uri here.
            // needs to resolve it to Pack://Application.
            //..\..\ in the relative Uri will get stripped when creating the new Uri and resolving to the
            //PackAppBaseUri, i.e. only relative Uri within the appbase are created here
            Uri currentUri = new Uri(BaseUriHelper.PackAppBaseUri, resourceLocator);

            //
            // Generate the ParserContext from packUri
            //
            ParserContext pc = new ParserContext();

            pc.BaseUri = currentUri;

            bool bCloseStream = true;  // Whether or not to close the stream after LoadBaml is done.

            Stream stream = null;  // stream could be extracted from the manifest resource or cached in the
                                   // LoadBamlSyncInfo depends on how this method is called.

            //
            // We could be here because of an InitializeCompoenent() call from the ctor of this component.
            // Check if this component was originally being created from the same Uri by the BamlConverter
            // or LoadComponent(uri).
            //
            if (IsComponentBeingLoadedFromOuterLoadBaml(currentUri) == true)
            {
                NestedBamlLoadInfo nestedBamlLoadInfo = s_NestedBamlLoadInfo.Peek();

                // If so, use the stream already created for this component on this thread by the
                // BamlConverter and seek to origin. This gives better perf by avoiding a duplicate
                // WebRequest.
                stream = nestedBamlLoadInfo.BamlStream;

                stream.Seek(0, SeekOrigin.Begin);

                pc.SkipJournaledProperties = nestedBamlLoadInfo.SkipJournaledProperties;

                // Reset the OuterUri in the top LoadBamlSyncInfo in the stack for the performance optimization.
                nestedBamlLoadInfo.BamlUri = null;

                // Start a new load context for this component to allow it to initialize normally via
                // its InitializeComponent() so that code in the ctor following it can access the content.
                // This call will not close the stream since it is owned by the load context created by
                // the BamlConverter and will be closed from that context after this function returns.

                bCloseStream = false;
            }
            else
            {
                // if not, this is a first time regular load of the component.
                PackagePart part = GetResourceOrContentPart(resourceLocator);
                ContentType contentType = new ContentType(part.ContentType);
                stream = part.GetSeekableStream();
                bCloseStream = true;

                //
                // The stream must be a BAML stream.
                // check the content type.
                //
                if (!MimeTypeMapper.BamlMime.AreTypeAndSubTypeEqual(contentType))
                {
                    throw new Exception(SR.Get(SRID.ContentTypeNotSupported, contentType));
                }
            }

            IStreamInfo bamlStream = stream as IStreamInfo;

            if (bamlStream == null || bamlStream.Assembly != component.GetType().Assembly)
            {
                throw new Exception(SR.Get(SRID.UriNotMatchWithRootType, component.GetType( ), resourceLocator));
            }

            XamlReader.LoadBaml(stream, pc, component, bCloseStream);
        }

        /// <summary>
        /// Create logic tree from given resource Locator, and return the root of
        /// this tree.
        /// </summary>
        /// <param name="resourceLocator">Resource Locator</param>
        public static object LoadComponent(Uri resourceLocator)
        {
            if (resourceLocator == null)
                throw new ArgumentNullException("resourceLocator");

            if (resourceLocator.OriginalString == null)
                throw new ArgumentException(SR.Get(SRID.ArgumentPropertyMustNotBeNull,"resourceLocator", "OriginalString"));

            if (resourceLocator.IsAbsoluteUri == true)
                throw new ArgumentException(SR.Get(SRID.AbsoluteUriNotAllowed));

            return LoadComponent(resourceLocator, false);
        }

        // <summary>
        // Create logic tree from given resource Locator, and return the root of
        // this tree.
        // </summary>
        // <param name="resourceLocator">Resource Locator</param>
        // <param name="bSkipJournaledProperties">SkipJournaledProperties or not</param>
        internal static object LoadComponent(Uri resourceLocator, bool bSkipJournaledProperties)
        {
            //
            // Only the Public LoadComponent(uri) and Journal navigation for PageFunction resume
            // call this method, the caller should have ensure the passed paramaters are valid.
            // So don't need to explicitly do the validation check for the parameters.
            //

            // Passed a relative Uri here.
            // needs to resolve it to Pack://Application.
            //..\..\ in the relative Uri will get stripped when creating the new Uri and resolving to the
            //PackAppBaseUri, i.e. only relative Uri within the appbase are created here
            Uri packUri = BindUriHelper.GetResolvedUri(BaseUriHelper.PackAppBaseUri, resourceLocator);

            PackagePart part = GetResourceOrContentPart(packUri);
            ContentType contentType = new ContentType(part.ContentType);
            Stream stream = part.GetSeekableStream();

            ParserContext pc = new ParserContext();
            pc.BaseUri = packUri;
            pc.SkipJournaledProperties = bSkipJournaledProperties;

            //
            // The stream must be a BAML or XAML stream.
            //
            if (MimeTypeMapper.BamlMime.AreTypeAndSubTypeEqual(contentType))
            {
                return LoadBamlStreamWithSyncInfo(stream, pc);
            }
            else if (MimeTypeMapper.XamlMime.AreTypeAndSubTypeEqual(contentType))
            {
                return XamlReader.Load(stream, pc);
            }
            else
            {
                throw new Exception(SR.Get(SRID.ContentTypeNotSupported, contentType.ToString()));
            }
        }


        //
        // A helper method to create a tree from a baml stream.
        //
        // This helper will be called by BamlConverter and LoadComponent(Uri).
        //
        // If root element type in the bamlstream was compiled from a xaml file,
        // and the bamlstream created from the xaml file is exact same as the baml stream
        // passed in this method, we don't want to generate duplicate tree nodes.
        // Navigate(Uri) and LoadComponent(Uri) may hit this situation.
        //
        // The caller should prepare baml stream and appropriate ParserContext.
        //
        internal static object LoadBamlStreamWithSyncInfo(Stream stream, ParserContext pc)
        {
            object rootElement = null;

            // The callers should have already done the parameter validation.
            // So the code here uses assert instead of throwing exception.
            //
            Debug.Assert(stream != null, "stream should not be null.");
            Debug.Assert(pc != null, "pc should not be null.");

            if (s_NestedBamlLoadInfo == null)
            {
                s_NestedBamlLoadInfo = new Stack<NestedBamlLoadInfo>();
            }

            // Keep the Uri and Stream which is being handled by LoadBaml in this thread.
            // If the ctor of the root element in this tree calls the InitializeComponent
            // method, the baml loaded in that context will go through and this one will
            // be skipped to completion automatically, since the same stream will be used,
            // essentially making the LoadBaml in this helper a No-OP.
            //
            NestedBamlLoadInfo loadBamlSyncInfo = new NestedBamlLoadInfo(pc.BaseUri, stream, pc.SkipJournaledProperties);

            s_NestedBamlLoadInfo.Push(loadBamlSyncInfo);
            try
            {
                // LoadBaml will close the stream.
                rootElement = XamlReader.LoadBaml(stream, pc, null, true);
            }
            finally
            {
                // Reset the per-thread Uri & Stream setting to indicate that tree generation
                // from the baml stream is done.
                s_NestedBamlLoadInfo.Pop();

                if (s_NestedBamlLoadInfo.Count == 0)
                {
                    s_NestedBamlLoadInfo = null;
                }
            }
            return rootElement;
        }

        /// <summary>
        /// Get PackagePart for a uri, the uri maps to a resource which is embedded
        /// inside manifest resource "$(AssemblyName).g.resources" in Application or
        /// dependent library assembly.
        ///
        /// If the Uri doesn't map to any resource, this method returns null.
        ///
        /// The accepted uri could be relative uri or pack uri.
        ///
        ///   Such as
        ///    Resource from Application assembly
        ///         "image/picture1.jpg" or
        ///         "pack://application:,,,/image/picture1.jpg"
        ///
        ///    Resource from a library assembly
        ///         "mylibrary;component/image/picture2.png" or
        ///         "pack://application:,,,/mylibrary;component/image/picture3.jpg"
        ///
        /// </summary>
        /// <param name="uriResource">the uri maps to the resource</param>
        /// <returns>PackagePart or null</returns>
        public static StreamResourceInfo GetResourceStream(Uri uriResource)
        {
            if (uriResource == null)
                throw new ArgumentNullException("uriResource");

            if (uriResource.OriginalString == null)
                throw new ArgumentException(SR.Get(SRID.ArgumentPropertyMustNotBeNull, "uriResource", "OriginalString"));

            if (uriResource.IsAbsoluteUri == true && !BaseUriHelper.IsPackApplicationUri(uriResource))
            {
                throw new ArgumentException(SR.Get(SRID.NonPackAppAbsoluteUriNotAllowed));
            }

            ResourcePart part = GetResourceOrContentPart(uriResource) as ResourcePart;
            return (part == null) ? null : new StreamResourceInfo(part.GetSeekableStream(), part.ContentType);
        }

        /// <summary>
        /// Get PackagePart for a uri, the uri maps to a content file which is associated
        /// with the application assembly.
        ///
        /// If the Uri doesn't map to any content file, this method returns null.
        ///
        /// The accepted uri could be relative uri or pack://Application:,,,/ uri.
        ///
        ///   Such as
        ///         "image/picture1.jpg"
        ///    or
        ///        "pack://application:,,,/image/picture1.jpg"
        ///
        /// </summary>
        /// <param name="uriContent">the uri maps to the Content File</param>
        /// <returns>PackagePart or null</returns>
        public static StreamResourceInfo GetContentStream(Uri uriContent)
        {
            if (uriContent == null)
                throw new ArgumentNullException("uriContent");

            if (uriContent.OriginalString == null)
                throw new ArgumentException(SR.Get(SRID.ArgumentPropertyMustNotBeNull, "uriContent", "OriginalString"));

            if (uriContent.IsAbsoluteUri == true && !BaseUriHelper.IsPackApplicationUri(uriContent))
            {
                throw new ArgumentException(SR.Get(SRID.NonPackAppAbsoluteUriNotAllowed));
            }

            ContentFilePart part = GetResourceOrContentPart(uriContent) as ContentFilePart;
            return (part == null) ? null : new StreamResourceInfo(part.GetSeekableStream(), part.ContentType);
        }

        /// <summary>
        /// Get PackagePart for a uri, the uri maps to a file which comes from the place
        /// where the application was originally deployed.
        ///
        /// The accepted uri could be relative uri such as "foo.jpg"
        ///
        /// or  pack Uri, "pack://siteoforigin:,,,/foo.jpg"
        ///
        /// </summary>
        /// <param name="uriRemote">the uri maps to the resource</param>
        /// <returns>PackagePart or null</returns>
        public static StreamResourceInfo GetRemoteStream(Uri uriRemote)
        {
            SiteOfOriginPart sooPart = null;

            if (uriRemote == null)
                throw new ArgumentNullException("uriRemote");

            if (uriRemote.OriginalString == null)
                throw new ArgumentException(SR.Get(SRID.ArgumentPropertyMustNotBeNull, "uriRemote", "OriginalString"));

            if (uriRemote.IsAbsoluteUri == true)
            {
                if (BaseUriHelper.SiteOfOriginBaseUri.IsBaseOf(uriRemote) != true)
                {
                    throw new ArgumentException(SR.Get(SRID.NonPackSooAbsoluteUriNotAllowed));
                }
            }

            Uri resolvedUri = BindUriHelper.GetResolvedUri(BaseUriHelper.SiteOfOriginBaseUri, uriRemote);

            Uri packageUri = PackUriHelper.GetPackageUri(resolvedUri);
            Uri partUri = PackUriHelper.GetPartUri(resolvedUri);

            //
            // SiteOfOriginContainer must have been added into the package cache, the code should just
            // take use of that SiteOfOriginContainer instance, instead of creating a new instance here.
            //
            SiteOfOriginContainer sooContainer = (SiteOfOriginContainer)GetResourcePackage(packageUri);

            // the SiteOfOriginContainer is shared across threads;  synchronize access to it
            // using the same lock object as other uses (PackWebResponse+CachedResponse.GetResponseStream)
            lock (sooContainer)
            {
                sooPart = sooContainer.GetPart(partUri) as SiteOfOriginPart;
            }

            //
            // Verify if the sooPart is for a valid remote file.
            //
            Stream stream = null;

            if (sooPart != null)
            {
                try
                {
                    stream = sooPart.GetSeekableStream();

                    if (stream == null)
                    {
                        //
                        // Cannot get stream from this PackagePart for some unknown reason,
                        // since the code didn't throw WebException.
                        //
                        sooPart = null;
                    }
                }
                catch (WebException)
                {
                    // A WebException is thrown when the code tried to get stream from the PackagePart.
                    // The Uri passed to this method must be a wrong uri.
                    // Return null for this case.
                    sooPart = null;

                    // For all other exceptions such as security exception, etc, let it go to upper frame,
                    // and fail eventually if no exception handler wants to catch it.
                }
            }

            // When stream is not null, sooPart cannot be null either
            Debug.Assert( ((stream != null) && (sooPart == null)) != true,  "When stream is not null, sooPart cannot be null either");

            return (stream == null) ? null : new StreamResourceInfo(stream, sooPart.ContentType);
        }

        /// <summary>
        /// Gets the cookie for the uri. It will work only for site of origin.
        /// </summary>
        /// <param name="uri">The uri for which the cookie is to be read</param>
        /// <returns>The cookie, if it exsits, else an exception is thrown.</returns>
        /// <Remarks>
        ///     Callers must have FileIOPermission(FileIOPermissionAccess.Read) or WebPermission(NetworkAccess.Connect) for the Uri, depending on whether the Uri is a file Uri or not, to call this API.
        /// </Remarks>
        public static string GetCookie(Uri uri)
        {
            return CookieHandler.GetCookie(uri, true/*throwIfNoCookie*/);
        }

        /// <summary>
        /// Sets the cookie for the uri. It will work only for site of origin.
        /// </summary>
        /// <param name="uri">The uri for which the cookie is to be set</param>
        /// <param name="value">The value of the cookie. Should be name=value, but "value-only" cookies are also allowed. </param>
        /// <Remarks>
        ///     Callers must have FileIOPermission(FileIOPermissionAccess.Read) or WebPermission(NetworkAccess.Connect) for the Uri, depending on whether the Uri is a file Uri or not, to call this API.
        /// </Remarks>
        public static void SetCookie(Uri uri, string value)
        {
            CookieHandler.SetCookie(uri, value);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     The Current property enables the developer to always get to the application in
        ///     AppDomain in which they are running.
        /// </summary>
        static public Application Current
        {
            get
            {
                // There is no need to take the _globalLock because reading a
                // reference is an atomic operation. Moreover taking a lock
                // also causes risk of re-entrancy because it pumps messages.

                return _appInstance;
            }
        }

        /// <summary>
        ///     The Windows property exposes a WindowCollection object, from which a developer
        ///     can iterate over all the windows that have been opened in the current application.
        /// </summary>
        // DO-NOT USE THIS PROPERY IF YOU MEAN TO MODIFY THE UNDERLYING COLLECTION.  USE
        // WindowsInternal PROPERTY FOR MODIFYING THE UNDERLYING DATASTRUCTURE.
        public WindowCollection Windows
        {
            get
            {
                VerifyAccess();
                return WindowsInternal.Clone();
            }
        }

        /// <summary>
        ///     The MainWindow property indicates the primary window of the application.
        ///     Note that setting the mainWindow property is not possible for browser hosted applications.
        /// </summary>
        /// <remarks>
        ///     By default - MainWindow will be set to the first window opened in the application.
        ///     However the MainWindow may be set programmatically to indicate "this is my main window".
        ///     It is a recommended programming style to refer to MainWindow in code instead of Windows[0].
        ///
        /// </remarks>
        public Window MainWindow
        {
            get
            {
                VerifyAccess();
                return _mainWindow;
            }

            set
            {
                VerifyAccess();

                if (value != _mainWindow)
                {
                    _mainWindow = value;
                }
            }
        }

        /// <summary>
        ///     The ShutdownMode property is called to set the shutdown specific mode of
        ///     the application. Setting this property controls the way in which an application
        ///     will shutdown.
        ///         The three values for the ShutdownMode enum are :
        ///                 OnLastWindowClose
        ///                 OnMainWindowClose
        ///                 OnExplicitShutdown
        ///
        ///         OnLastWindowClose - this mode will shutdown the application when  the
        ///                             last window is closed, or an explicit call is made
        ///                             to Application.Shutdown(). This is the default mode.
        ///
        ///         OnMainWindowClose - this mode will shutdown the application when the main
        ///                             window has been closed, or Application.Shutdown() is
        ///                             called. Note that if the MainWindow property has not
        ///                             been set - this mode is equivalent to OnExplicitOnly.
        ///
        ///         OnExplicitShutdown- this mode will shutdown the application only when an
        ///                             explicit call to OnShutdown() has been made.
        /// </summary>
        public ShutdownMode ShutdownMode
        {
            get
            {
                VerifyAccess();
                return _shutdownMode;
            }

            set
            {
                VerifyAccess();
                if ( !IsValidShutdownMode(value) )
                {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ShutdownMode));
                }
                if (IsShuttingDown == true || _appIsShutdown == true)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ShutdownModeWhenAppShutdown));
                }

                _shutdownMode = value;
            }
        }

        /// <summary>
        ///     Current locally defined Resources
        /// </summary>
        [Ambient]
        public ResourceDictionary Resources
        {
            //Don't use  VerifyAccess() here since Resources can be set from any thread.
            //We synchronize access using _globalLock

            get
            {
                ResourceDictionary resources;
                bool needToAddOwner = false;

                lock(_globalLock)
                {
                    if (_resources == null)
                    {
                        // Shouldn't return null for property of type collection.
                        // It enables the Mort scenario: application.Resources.Add();
                        _resources = new ResourceDictionary();
                        needToAddOwner = true;
                    }

                    resources = _resources;
                }

                if (needToAddOwner)
                {
                    resources.AddOwner(this);
                }

                return resources;
            }
            set
            {
                bool invalidateResources = false;
                ResourceDictionary oldValue;

                lock(_globalLock)
                {
                    oldValue = _resources;
                    _resources = value;
                }

                if (oldValue != null)
                {
                    // This app is no longer an owner for the old RD
                    oldValue.RemoveOwner(this);
                }

                if (value != null)
                {
                    if (!value.ContainsOwner(this))
                    {
                        // This app is an owner for the new RD
                        value.AddOwner(this);
                    }
                }

                if (oldValue != value)
                {
                    invalidateResources = true;
                }

                if (invalidateResources)
                {
                    InvalidateResourceReferences(new ResourcesChangeInfo(oldValue, value));
                }
            }
        }

        ResourceDictionary IHaveResources.Resources
        {
            get { return Resources; }
            set { Resources = value; }
        }

        bool IQueryAmbient.IsAmbientPropertyAvailable(string propertyName)
        {
            // We want to make sure that StaticResource resolution checks the .Resources
            // Ie.  The Ambient search should look at Resources if it is set.
            // Even if it wasn't set from XAML (eg. the Ctor (or derived Ctor) added stuff)
            return (propertyName == "Resources" && _resources != null);
        }

        // Says if App.Resources has any implicit styles
        internal bool HasImplicitStylesInResources
        {
            get { return _hasImplicitStylesInResources; }
            set { _hasImplicitStylesInResources = value; }
        }

        /// <summary>
        /// The main page
        /// </summary>
        /// <remarks>
        /// This property should only be called from the Application thread
        /// </remarks>
        public Uri StartupUri
        {
            get
            {
                // Don't call VerifyAccess.  System.Printing can call this from another thread.
                return _startupUri;
            }
            set
            {
                VerifyAccess();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _startupUri = value;
            }
        }


        /// <summary>
        /// Gets the property Hashtable.
        /// </summary>
        /// <returns>IDictionary interface</returns>
        /// <remarks>
        /// This property is accessible from any thread
        /// </remarks>
        public IDictionary Properties
        {
            get
            {
                // change from before; Before we used to return null
                // but that might throw a nullpointer exception if
                // indexers are being used on the Hashtable object
                lock(_globalLock)
                {
                    if (_htProps == null)
                    {
                        // so we will have 5 entries before we resize
                        _htProps = new HybridDictionary(5);
                    }
                    return _htProps;
                }
            }
        }


        /// <summary>
        /// This property gets and sets the assembly pack://application:,,,/ refers to.
        /// </summary>
        public static Assembly ResourceAssembly
        {
            get
            {
                if (_resourceAssembly == null)
                {
                    lock (_globalLock)
                    {
                        _resourceAssembly = Assembly.GetEntryAssembly();
                    }
                }

                return _resourceAssembly;
            }
            set
            {
                lock (_globalLock)
                {
                    if (_resourceAssembly != value)
                    {
                        if ((_resourceAssembly == null) && (Assembly.GetEntryAssembly() == null))
                        {
                            _resourceAssembly = value;
                            BaseUriHelper.ResourceAssembly = value;
                        }
                        else
                        {
                            throw new InvalidOperationException(SR.Get(SRID.PropertyIsImmutable, "ResourceAssembly", "Application"));
                        }
                    }
                }
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events
        /// <summary>
        ///     The Startup event is fired when an application is starting.
        ///     This event is raised by the OnStartup method.
        /// </summary>
        public event StartupEventHandler Startup
        {
            add{ VerifyAccess(); Events.AddHandler(EVENT_STARTUP, value); }
            remove{ VerifyAccess(); Events.RemoveHandler(EVENT_STARTUP, value); }
        }

        /// <summary>
        /// The Exit event is fired when an application is shutting down.
        /// This event is raised by the OnExit method.
        /// </summary>
        public event ExitEventHandler Exit
        {
            add{ VerifyAccess(); Events.AddHandler(EVENT_EXIT, value); }
            remove{ VerifyAccess(); Events.RemoveHandler(EVENT_EXIT, value); }
        }

        /// <summary>
        /// The Activated event is fired when an applications window has been activated from
        /// the OS ( alt-tab, or changing application from taskbar, or clicking on a winodw).
        /// This event is raised by the OnActivated method.
        /// </summary>
        public event EventHandler Activated;

        /// <summary>
        /// The Deactivated event is fired when an applications window has been de-activated
        /// from the OS ( alt-tab, or changing application from taskbar, or clicking away
        /// from an applications window). This event is raised by the OnDeactivated method.
        /// </summary>
        public event EventHandler Deactivated;

        /// <summary>
        /// The SessionEnding event is fired when windows is ending, either due to a shutdown,
        /// or loggoff from the start menu ( or calling the ExitWindows function).  The
        /// ReasonSessionEnding enum on the  SessionEndingEventArgs indicates whether the session
        /// is ending in response to a shutdown of the OS, or if the user is logging off.
        /// </summary>
        public event SessionEndingCancelEventHandler SessionEnding
        {
            add{ VerifyAccess(); Events.AddHandler(EVENT_SESSIONENDING, value); }
            remove{ VerifyAccess(); Events.RemoveHandler(EVENT_SESSIONENDING, value); }
        }

        /// <summary>
        /// The DispatcherUnhandledException event is fired when an unhandled exception
        /// is caught at the Dispatcher level (by the dispatcher).
        /// </summary>
        public event DispatcherUnhandledExceptionEventHandler DispatcherUnhandledException
        {
            //Dispatcher.Invoke will call the callback on the dispatcher thread so a VerifyAccess()
            //check is redundant (and wrong) outside the invoke call

            add
            {
                Dispatcher.Invoke(
                    DispatcherPriority.Send,
                    (DispatcherOperationCallback) delegate(object unused)
                    {
                        Dispatcher.UnhandledException += value;
                        return null;
                    },
                    null);
            }
            remove
            {
                Dispatcher.Invoke(
                    DispatcherPriority.Send,
                    (DispatcherOperationCallback) delegate(object unused)
                    {
                        Dispatcher.UnhandledException -= value;
                        return null;
                    },
                    null);
            }
        }

        /// <summary>
        /// </summary>
        public event NavigatingCancelEventHandler Navigating;

        /// <summary>
        /// </summary>
        public event NavigatedEventHandler Navigated;

        /// <summary>
        /// </summary>
        public event NavigationProgressEventHandler NavigationProgress;

        /// <summary>
        /// This event is fired when an error is encountered during a navigation
        /// </summary>
        public event NavigationFailedEventHandler NavigationFailed;

        /// <summary>
        /// </summary>
        public event LoadCompletedEventHandler LoadCompleted;

        /// <summary>
        /// </summary>
        public event NavigationStoppedEventHandler NavigationStopped;

        /// <summary>
        /// </summary>
        public event FragmentNavigationEventHandler FragmentNavigation;
        #endregion Public Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods
        /// <summary>
        ///     OnStartup is called to raise the Startup event. The developer will typically override this method
        ///     if they want to take action at startup time ( or they may choose to attach an event).
        ///     This method will be called once when the application begins, once that application's Run() method
        ///     has been called.
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual method
        ///     that raises an event, to provide a convenience for developers that subclass the event.
        ///     If you override this method - you need to call Base.OnStartup(...) for the corresponding event
        ///     to be raised.
        /// </remarks>
        /// <param name="e">The event args that will be passed to the Startup event</param>
        protected virtual void OnStartup(StartupEventArgs e)
        {
            VerifyAccess();

            StartupEventHandler handler = (StartupEventHandler)Events[EVENT_STARTUP];
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        ///     OnExit is called to raise the Exit event.
        ///     The developer will typically override this method if they want to take
        ///     action when the application exits  ( or they may choose to attach an event).
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual method
        ///     that raises an event, to provide a convenience for developers that subclass the event.
        ///     If you override this method - you need to call Base.OnExit(...) for the
        ///     corresponding event to be raised.
        /// </remarks>
        /// <param name="e">The event args that will be passed to the Exit event</param>
        protected virtual void OnExit(ExitEventArgs e)
        {
            VerifyAccess();

            ExitEventHandler handler = (ExitEventHandler)Events[EVENT_EXIT];
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        ///     OnActivated is called to raise the Activated event.
        ///     The developer will typically override this method if they want to take action
        ///     when the application gets activated ( or they may choose to attach an event).
        ///     This method will be called when one of the current applications windows gets
        ///     activated on the desktop. ( This corresponds to Users WM_ACTIVATEAPP message).
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected
        ///     virtual method that raises an event, to provide a convenience for developers
        ///     that subclass the event.
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnActivated(EventArgs e)
        {
            VerifyAccess();
            if (Activated != null)
            {
                Activated(this, e);
            }
        }

        /// <summary>
        ///     OnDeactivated is called to raise the Deactivated event. The developer will
        ///     typically override this method if they want to take action when the application
        ///     gets deactivated ( or they may choose to attach an event).
        ///     This method will be called when one of the current applications windows gets
        ///     activated on the desktop. ( This corresponds to Users WM_ACTIVATEAPP message,
        ///     with an wparam indicating the app is being deactivated).
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that
        ///     subclass the event.
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnDeactivated(EventArgs e)
        {
            VerifyAccess();
            if (Deactivated != null)
            {
                Deactivated(this, e);
            }
        }

        /// <summary>
        ///     OnSessionEnding is called to raise the SessionEnding event. The developer will
        ///     typically override this method if they want to take action when the OS is ending
        ///     a session ( or they may choose to attach an event). This method will be called when
        ///     the user has chosen to either logoff or shutdown. These events are equivalent
        ///     to receiving a WM_QUERYSESSION window event. Windows will send it when user is
        ///     logging out/shutting down. ( See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/sysinfo/base/wm_queryendsession.asp ).
        ///     By default if this event is not cancelled - Avalon will then call Application.Shutdown.
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event.
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            VerifyAccess();

            SessionEndingCancelEventHandler handler = (SessionEndingCancelEventHandler)Events[EVENT_SESSIONENDING];
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// This method fires the Navigating event
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnNavigating(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e">NavigationEventArgs</param>
        protected virtual void OnNavigating(NavigatingCancelEventArgs e)
        {
            VerifyAccess();
            if (Navigating != null)
            {
                Navigating(this, e);
            }
        }

        /// <summary>
        /// This method fires the Navigated event
        /// on receiving all of the stream contents
        /// for the given bpu
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnNavigated(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e">NavigationEventArgs</param>
        protected virtual void OnNavigated(NavigationEventArgs e)
        {
            VerifyAccess();
            if (Navigated != null)
            {
                Navigated(this, e);
            }
        }

        /// <summary>
        /// This method fires the NavigationProgress event
        /// each time number of bytes equal to bytesInterval is read
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnNavigationProgress(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e">NavigationEventArgs</param>
        protected virtual void OnNavigationProgress(NavigationProgressEventArgs e)
        {
            VerifyAccess();
            if (NavigationProgress != null)
            {
                NavigationProgress(this, e);
            }
        }

        /// <summary>
        /// This method fires the NavigationFailed event
        /// When there is an error encountered in navigation
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnNavigationProgress(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e">NavigationFailedEventArgs</param>
        protected virtual void OnNavigationFailed(NavigationFailedEventArgs e)
        {
            VerifyAccess();
            if (NavigationFailed != null)
            {
                NavigationFailed(this, e);
            }
        }

        /// <summary>
        /// This method fires the LoadCompleted event
        /// after parsing all of the top level page
        /// and its secondary contents
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnLoadCompleted(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e">NavigationEventArgs</param>
        protected virtual void OnLoadCompleted(NavigationEventArgs e)
        {
            VerifyAccess();
            if (LoadCompleted != null)
            {
                LoadCompleted(this, e);
            }
        }

        /// <summary>
        /// This method fires the Stopped event
        /// whenever the top level has navigated and
        /// there occurs a stop after that
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnNavigationStopped(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e">NavigationEventArgs</param>
        protected virtual void OnNavigationStopped(NavigationEventArgs e)
        {
            VerifyAccess();
            if (NavigationStopped != null)
            {
                NavigationStopped(this, e);
            }
        }


        /// <summary>
        /// This method fires the FragmentNavigation event
        /// whenever a navigation to a uri containing a fragment
        /// occurs.
        /// </summary>
        /// <remarks>
        ///     This method follows the .Net programming guideline of having a protected virtual
        ///     method that raises an event, to provide a convenience for developers that subclass
        ///     the event. If you override this method - you need to call Base.OnNavigationStopped(...) for
        ///     the corresponding event to be raised.
        /// </remarks>
        /// <param name="e">FragmentNavigationEventArgs</param>
        protected virtual void OnFragmentNavigation(FragmentNavigationEventArgs e)
        {
            VerifyAccess();
            if (FragmentNavigation != null)
            {
                FragmentNavigation(this, e);
            }
        }
        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // It would be nice to make this FamANDAssem (protected and internal) if c# supported it
        internal virtual void PerformNavigationStateChangeTasks(
            bool isNavigationInitiator, bool playNavigatingSound, NavigationStateChange state)
        {
            if (isNavigationInitiator)
            {
                switch (state)
                {
                    case NavigationStateChange.Navigating:
                        if (playNavigatingSound)
                        {
                            PlaySound(SOUND_NAVIGATING);
                        }
                        break;
                    case NavigationStateChange.Completed:
                        PlaySound(SOUND_COMPLETE_NAVIGATION);
                        break;
                    case NavigationStateChange.Stopped:
                        break;
                }
            }
        }


        /// <summary>
        /// Application Startup.
        /// </summary>
        internal void DoStartup()
        {
            Debug.Assert(CheckAccess(), "This should only be called on the Application thread");

            if (StartupUri != null)
            {
                if (StartupUri.IsAbsoluteUri == false)
                {
                    // Resolve it against the ApplicationMarkupBaseUri.
                    StartupUri = new Uri(ApplicationMarkupBaseUri, StartupUri);
                }

                // ArrowHead Optimization for StartupUri:
                // When loading app resources (pack://application, we do not need to go through the navigation logic.
                // We can load the stream through ResourceContainer directly. This way we avoid loading the navigation
                // and webrequest code. It is also sync, instead of async if going through navigation code pass.
                // This optimization saves about 3-4% cold startup time for a simple application.
                // However, we need to maintain back compact in the following areas until we can do breaking change.
                // 1. Continue to support other uri and content types (the else statement).
                // 2. Continue to fire Navigating events.
                if (BaseUriHelper.IsPackApplicationUri(StartupUri))
                {
                    // BACK COMPAT:
                    // We need to fire Navigating event to be back compact with V1.
                    // We should drop this when we can do breaking change.
                    Uri relativeUri = BindUriHelper.GetUriRelativeToPackAppBase(StartupUri);
                    NavigatingCancelEventArgs e = new NavigatingCancelEventArgs(relativeUri, null, null, null, NavigationMode.New, null, null, true);
                    FireNavigating(e, true);

                    // Navigating can be cancelled.
                    if (! e.Cancel)
                    {
                        object root = LoadComponent(StartupUri, false);

                        // If the root element is not a window, we need to create a window.
                        ConfigAppWindowAndRootElement(root, StartupUri);
                    }
                }
                else
                {
                    // BACK COMPAT:
                    // The following logic is for pack://siteoforign uri syntax and all other uri and content types
                    // that the WPF navigation framework supports, including Html.
                    //
                    // We want to maintain the V1 behavior to continue support loading those content. I suggest reconsidering
                    // this support when we can do breaking change. We need to understand what scenarios require
                    // the Application StartupUri to load content other than xaml/baml in the app resource or content file.
                    // If there are no interesting ones, we should remove this support.
                    NavService = new NavigationService(null);
                    NavService.AllowWindowNavigation = true;
                    NavService.PreBPReady += new BPReadyEventHandler(OnPreBPReady);
                    NavService.Navigate(StartupUri);
                }
            }
        }

        /// <summary>
        /// DO NOT USE - internal method
        /// </summary>
        internal virtual void DoShutdown()
        {
            Debug.Assert(CheckAccess() == true, "DoShutdown can only be called on the Dispatcer thread");
            // We need to know if we have been shut down already.
            // We cannot check the IsShuttingDown variable because it is set true
            // in the function that calls us.

            // We use a while loop like this because closing a window will modify the windows list.
            while(WindowsInternal.Count > 0)
            {
                if (!WindowsInternal[0].IsDisposed)
                {
                    WindowsInternal[0].InternalClose(true, true);
                }
                else
                {
                    WindowsInternal.RemoveAt(0);
                }
            }
            WindowsInternal = null;

            ExitEventArgs e = new ExitEventArgs(_exitCode);

            // Event handler exception continuality: if exception occurs in ShuttingDown event handler,
            // our cleanup action is to finish Shuttingdown.  Since Shuttingdown cannot be cancelled.
            // We don't want user to use throw exception and catch it to cancel Shuttingdown.
            try
            {
                // fire Applicaiton Exit event
                OnExit(e);
            }
            finally
            {
                SetExitCode(e._exitCode);

                // By default statics are shared across appdomains, so need to clear
                lock (_globalLock)
                {
                    _appInstance = null;
                }

                _mainWindow = null;
                _htProps = null;
                NonAppWindowsInternal = null;

                // this will always be null in the browser hosted case since we we don't
                // support Activate, Deactivate, and SessionEnding events in the
                // browser scenario and thus we never create this hwndsource.
                if (_parkingHwnd != null)
                {
                    _parkingHwnd.Dispose();
                }

                if (_events != null)
                {
                    _events.Dispose();
                }

                PreloadedPackages.Clear();
                AppSecurityManager.ClearSecurityManager();

                _appIsShutdown = true; // mark app as shutdown
            }
        }

        //
        // This function is called from the public Run methods to start the application.
        // ApplicationProxyInternal.Run method calls this method directly to bypass the check
        // for browser hosted application in the public Run() method
        //
        internal int RunInternal(Window window)
        {
            VerifyAccess();

#if DEBUG_CLR_MEM
            if (CLRProfilerControl.ProcessIsUnderCLRProfiler &&
               (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
            {
                CLRProfilerControl.CLRLogWriteLine("Application_Run");
            }
#endif // DEBUG_CLR_MEM

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientAppRun);

            //
            // (Can't create app and do run/shutdown followed
            // by run/shutdown)
            //
            // Devs could write the following code
            //
            // Application app = new Application();
            // app.Run();
            // app.Run();
            //
            // In this case, we should throw an exception when Run is called for the second time.
            // When app is shutdown, _appIsShutdown is set to true.  If it is true here, then we
            // throw an exception
            if (_appIsShutdown == true)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotCallRunMultipleTimes, this.GetType().FullName));
            }

            if (window != null)
            {
                if (window.CheckAccess() == false)
                {
                    throw new ArgumentException(SR.Get(SRID.WindowPassedShouldBeOnApplicationThread, window.GetType().FullName, this.GetType().FullName));
                }

                if (WindowsInternal.HasItem(window) == false)
                {
                    WindowsInternal.Add(window);
                }

                if (MainWindow == null)
                {
                    MainWindow = window;
                }

                if (window.Visibility != Visibility.Visible)
                {
                    Dispatcher.BeginInvoke(
                        DispatcherPriority.Send,
                        (DispatcherOperationCallback) delegate(object obj)
                        {
                            Window win = obj as Window;
                            win.Show();
                            return null;
                        },
                        window);
                }
            }

            EnsureHwndSource();

            //Even if the subclass app cancels the event we still want to create and run the dispatcher
            //so that when the app explicitly calls Shutdown, we have a dispatcher to service the posted
            //Shutdown DispatcherOperationCallback

            // Invoke the Dispatcher synchronously if we are not in the browser
            RunDispatcher(null);

            return _exitCode;
        }

        internal void InvalidateResourceReferences(ResourcesChangeInfo info)
        {
            // Invalidate ResourceReference properties on all the windows.
            // we Clone() the collection b/c if we don't then some other thread can be
            // modifying the collection while we iterate over it
            InvalidateResourceReferenceOnWindowCollection(WindowsInternal.Clone(), info);
            InvalidateResourceReferenceOnWindowCollection(NonAppWindowsInternal.Clone(), info);
        }

        // Creates and returns a NavigationWindow for standalone cases
        // For browser hosted cases, returns the existing RootBrowserWindow which
        //   is created before the application.Run is called.
        internal NavigationWindow GetAppWindow()
        {
            NavigationWindow appWin = new NavigationWindow();

            // We don't want to show the window before the content is ready, but for compatibility reasons
            // we do want it to have an HWND available.  Not doing this can cause Application's MainWindow
            // to be null when LoadCompleted has happened.
            new WindowInteropHelper(appWin).EnsureHandle();

            return appWin;
        }


        internal void FireNavigating(NavigatingCancelEventArgs e, bool isInitialNavigation)
        {
            PerformNavigationStateChangeTasks(e.IsNavigationInitiator, !isInitialNavigation, NavigationStateChange.Navigating);

            OnNavigating(e);
        }

        internal void FireNavigated(NavigationEventArgs e)
        {
            OnNavigated(e);
        }

        internal void FireNavigationProgress(NavigationProgressEventArgs e)
        {
            OnNavigationProgress(e);
        }

        internal void FireNavigationFailed(NavigationFailedEventArgs e)
        {
            // Browser downloading state not reset; case 1.
            PerformNavigationStateChangeTasks(true, false, NavigationStateChange.Stopped);

            OnNavigationFailed(e);
        }

        internal void FireLoadCompleted(NavigationEventArgs e)
        {
            PerformNavigationStateChangeTasks(e.IsNavigationInitiator, false, NavigationStateChange.Completed);

            OnLoadCompleted(e);
        }

        internal void FireNavigationStopped(NavigationEventArgs e)
        {
            PerformNavigationStateChangeTasks(e.IsNavigationInitiator, false, NavigationStateChange.Stopped);

            OnNavigationStopped(e);
        }

        internal void FireFragmentNavigation(FragmentNavigationEventArgs e)
        {
            OnFragmentNavigation(e);
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // The public Windows property returns a copy of the underlying
        // WindowCollection.  This property is used internally to enable
        // modyfying the underlying collection.
        internal WindowCollection WindowsInternal
        {
            get
            {
                lock(_globalLock)
                {
                    if(_appWindowList == null)
                    {
                        _appWindowList = new WindowCollection();
                    }
                    return _appWindowList;
                }
            }
            private set
            {
                lock(_globalLock)
                {
                    _appWindowList = value;
                }
            }
        }

        internal WindowCollection NonAppWindowsInternal
        {
            get
            {
                lock(_globalLock)
                {
                    if (_nonAppWindowList == null)
                    {
                        _nonAppWindowList = new WindowCollection();
                    }
                    return _nonAppWindowList;
                }
            }

            private set
            {
                lock(_globalLock)
                {
                    _nonAppWindowList = value;
                }
            }
        }

        //This property indicates what type of an application was created. We use this
        //to determine whether to update the address bar or not for toplevel navigations
        //Since we don't currently have support to represent a proper relative uri
        //for .xps or .deploy or browser hosted exes, we limit address bar
        //updates to xaml navigations.
        //In the future, IBrowserCallbackServices and this should be moved to use RootBrowserWindow
        //instead of being in the application. For example,if a standalone window is created
        //in the same application, we still try to use IBrowserCallbackServices in the
        //standalone window. Need to ensure only RootBrowserWindow knows about browser hosting,
        //rest of the appmodel code should be agnostic to hosting process.
        //This will be cleaned up with the RootBrowserWindow cleanup.
        internal MimeType MimeType
        {
            get { return _appMimeType.Value; }
            set { _appMimeType = new SecurityCriticalDataForSet<MimeType>(value); }
        }

        // this is called from ApplicationProxyInternal, ProgressBarAppHelper, and ContainerActivationHelper.
        // All of these are on the app thread
        internal IServiceProvider ServiceProvider
        {
            private get
            {
                VerifyAccess();
                if (_serviceProvider != null)
                {
                    return _serviceProvider;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                VerifyAccess();
                _serviceProvider = value ;
            }
        }


        // is called by NavigationService to detect TopLevel container
        // We check there to call this only if NavigationService is on
        // the same thread as the Application
        internal NavigationService NavService
        {
            get
            {
                VerifyAccess();
                return _navService;
            }
            set
            {
                VerifyAccess();
                _navService = value;
            }
        }

        internal static bool IsShuttingDown
        {
            get
            {
                //If we are shutting down normally, Application.IsShuttingDown will be true. Be sure to check this first.
                // If we are browser hosted, BrowserCallbackServices.IsShuttingDown checks to see if the browser is shutting us down,
                // even if we may not be shutting down the Application yet. Check this to avoid reentrance issues between the time that
                // browser is shutting us down and that Application.Shutdown (CriticalShutdown) is invoked.
                if (_isShuttingDown)
                {
                    return _isShuttingDown;
                }

                return false;
            }
            set
            {
                lock(_globalLock)
                {
                    _isShuttingDown = value;
                }
            }
        }

        // This returns the static variable _isShuttingDown.
        internal static bool IsApplicationObjectShuttingDown
        {
            get
            {
                return _isShuttingDown;
            }
        }

        /// <summary>
        /// Returns the handle of the parking window.
        /// </summary>
        internal IntPtr ParkingHwnd
        {
            get
            {
                if (_parkingHwnd != null)
                {
                    return _parkingHwnd.Handle;
                }
                else
                {
                    return IntPtr.Zero;
                }
            }
        }

        //
        // Keep the BaseUri for the application definition xaml markup stream.
        //
        // If the appdef xaml file is not in the project root directory, the baseUri for this
        // xaml file is not same as PackAppBaseUri.
        //
        // If StartupUri is set to a relative Uri, it should be resolved against this
        // ApplicationMarkupBaseUri before the uri is passed to NavigationService methods.
        //
        internal Uri ApplicationMarkupBaseUri
        {
            get
            {
                if (_applicationMarkupBaseUri == null)
                {
                    _applicationMarkupBaseUri = BaseUriHelper.BaseUri;
                }

                return _applicationMarkupBaseUri;
            }

            set
            {
                _applicationMarkupBaseUri = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods
        // <summary>
        // Sets up several things on startup.  If you want to use avalon services without using the
        // Application class you will need to call this method explicitly.  Standard avalon applications
        // will not have to worry about this detail.
        // </summary>
        private static void ApplicationInit()
        {
            _globalLock = new object();

            // Add an instance of the ResourceContainer to PreloadedPackages so that PackWebRequestFactory can find it
            // and mark it as thread-safe so PackWebResponse won't protect returned streams with a synchronizing wrapper
            PreloadedPackages.AddPackage(PackUriHelper.GetPackageUri(BaseUriHelper.PackAppBaseUri), new ResourceContainer(), true);

            MimeObjectFactory.Register(MimeTypeMapper.BamlMime, new StreamToObjectFactoryDelegate(AppModelKnownContentFactory.BamlConverter));

            StreamToObjectFactoryDelegate xamlFactoryDelegate = new StreamToObjectFactoryDelegate(AppModelKnownContentFactory.XamlConverter);

            MimeObjectFactory.Register(MimeTypeMapper.XamlMime, xamlFactoryDelegate);
            MimeObjectFactory.Register(MimeTypeMapper.FixedDocumentMime, xamlFactoryDelegate);
            MimeObjectFactory.Register(MimeTypeMapper.FixedDocumentSequenceMime, xamlFactoryDelegate);
            MimeObjectFactory.Register(MimeTypeMapper.FixedPageMime, xamlFactoryDelegate);
            MimeObjectFactory.Register(MimeTypeMapper.ResourceDictionaryMime, xamlFactoryDelegate);

            StreamToObjectFactoryDelegate htmlxappFactoryDelegate = new StreamToObjectFactoryDelegate(AppModelKnownContentFactory.HtmlXappConverter);
            MimeObjectFactory.Register(MimeTypeMapper.HtmMime, htmlxappFactoryDelegate);
            MimeObjectFactory.Register(MimeTypeMapper.HtmlMime, htmlxappFactoryDelegate);
            MimeObjectFactory.Register(MimeTypeMapper.XbapMime, htmlxappFactoryDelegate);
        }

        // This function returns the resource stream including resource and content file.
        // This is called by GetContentStream and GetResourceStream.
        // NOTE: when we can do breaking change, we should consider uniting GetContentStream
        // with GetResourceStream. Developer should not need to know and be able to get the
        // stream based on the uri (pack application).
        private static PackagePart GetResourceOrContentPart(Uri uri)
        {
            // Caller examines the input parameter.
            // It should be either a relative or pack application uri here.
            Debug.Assert(!uri.IsAbsoluteUri || BaseUriHelper.IsPackApplicationUri(uri));
            Uri packAppUri = BaseUriHelper.PackAppBaseUri;
            Uri resolvedUri = BindUriHelper.GetResolvedUri(packAppUri, uri);

            Uri packageUri = PackUriHelper.GetPackageUri(resolvedUri);
            Uri partUri = PackUriHelper.GetPartUri(resolvedUri);
            
            //
            // ResourceContainer must have been added into the package cache, the code should just
            // take use of that ResourceContainer instance, instead of creating a new instance here.
            //
            ResourceContainer resContainer = (ResourceContainer)GetResourcePackage(packageUri);

            // the ResourceContainer is shared across threads;  synchronize access to it
            // using the same lock object as other uses (PackWebResponse+CachedResponse.GetResponseStream)
            PackagePart part = null;
            lock (resContainer)
            {
                part = resContainer.GetPart(partUri);
            }

            return part;
        }

        /// <summary> Helper for getting the pack://application or pack://siteoforigin resource package. </summary>
        /// <param name="packageUri"> "application://" or "siteoforigin://" </param>
        private static Package GetResourcePackage(Uri packageUri)
        {
            Package package = PreloadedPackages.GetPackage(packageUri);
            if (package == null)
            {
                Uri packUri = PackUriHelper.Create(packageUri);
                Invariant.Assert(packUri == BaseUriHelper.PackAppBaseUri || packUri == BaseUriHelper.SiteOfOriginBaseUri,
                                "Unknown packageUri passed: "+packageUri);

                Invariant.Assert(IsApplicationObjectShuttingDown);
                throw new InvalidOperationException(SR.Get(SRID.ApplicationShuttingDown));
            }
            return package;
        }

        /// <summary>
        ///     Creates hwndsource so that we can listen to some window msgs.
        /// </summary>
        private void EnsureHwndSource()
        {
            if (_parkingHwnd == null)
            {
                // _appFilterHook needs to be member variable otherwise
                // it is GC'ed and we don't get messages from HwndWrapper
                // (HwndWrapper keeps a WeakReference to the hook)

                _appFilterHook = new HwndWrapperHook(AppFilterMessage);
                HwndWrapperHook[] wrapperHooks = {_appFilterHook};

                _parkingHwnd = new HwndWrapper(
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                "",
                                IntPtr.Zero,
                                wrapperHooks);
            }
        }

        private IntPtr AppFilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr retInt = IntPtr.Zero;
            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_ACTIVATEAPP:
                    handled = WmActivateApp(NativeMethods.IntPtrToInt32(wParam));
                    break;
                case WindowMessage.WM_QUERYENDSESSION :
                    handled = WmQueryEndSession(lParam, ref retInt);
                    break;
                default:
                    handled = false;
                    break;
            }
            return retInt;
        }

        private bool WmActivateApp(Int32 wParam)
        {
            int temp = wParam;
            bool isActivated = (temp == 0? false : true);

            // Event handler exception continuality: if exception occurs in Activate/Deactivate event handlers, our state would not
            // be corrupted because no internal state are affected by Activate/Deactivate. Please check Event handler exception continuality
            // if a state depending on those events is added.
            if (isActivated == true)
            {
                OnActivated(EventArgs.Empty);
            }
            else
            {
                OnDeactivated(EventArgs.Empty);
            }
            return false;
        }

        private bool WmQueryEndSession(IntPtr lParam, ref IntPtr refInt)
        {
            int reason = NativeMethods.IntPtrToInt32(lParam);
            bool retVal = false;

            // Event handler exception continuality: if exception occurs in SessionEnding event handlers, our state would not
            // be corrupted because no internal state are affected by SessionEnding. Please check Event handler exception continuality
            // if a state depending on this event is added.
            SessionEndingCancelEventArgs secEventArgs = new SessionEndingCancelEventArgs( (reason & NativeMethods.ENDSESSION_LOGOFF) != 0? ReasonSessionEnding.Logoff : ReasonSessionEnding.Shutdown );
            OnSessionEnding( secEventArgs );

            // shut down the app if not cancelled
            if ( secEventArgs.Cancel == false )
            {
                Shutdown();
                // return true to the wnd proc to signal that we can terminate properly
                refInt = new IntPtr(1);
                retVal = false;
            }
            else
            {
                refInt = IntPtr.Zero;

                // we have handled the event DefWndProc will not be called for this msg
                retVal = true;
            }

            return retVal;
        }

        private void InvalidateResourceReferenceOnWindowCollection(WindowCollection wc, ResourcesChangeInfo info)
        {
            bool hasImplicitStyles  = info.IsResourceAddOperation && HasImplicitStylesInResources;

            for (int i = 0; i < wc.Count; i++)
            {
                // calling thread is the same as the wc[i] thread so synchronously invalidate
                // resouces, else, post a dispatcher workitem to invalidate resources.
                if (wc[i].CheckAccess() == true)
                {
                    // Set the ShouldLookupImplicitStyles flag on the App's windows
                    // to true if App.Resources has implicit styles.

                    if (hasImplicitStyles)
                        wc[i].ShouldLookupImplicitStyles = true;

                    TreeWalkHelper.InvalidateOnResourcesChange(wc[i], null, info);
                }
                else
                {
                    wc[i].Dispatcher.BeginInvoke(
                        DispatcherPriority.Send,
                        (DispatcherOperationCallback) delegate(object obj)
                        {
                            object[] args = obj as object[];

                            // Set the ShouldLookupImplicitStyles flag on the App's windows
                            // to true if App.Resources has implicit styles.

                            if (hasImplicitStyles)
                                ((FrameworkElement)args[0]).ShouldLookupImplicitStyles = true;

                            TreeWalkHelper.InvalidateOnResourcesChange((FrameworkElement)args[0], null, (ResourcesChangeInfo)args[1]);
                            return null;
                        },
                        new object[] {wc[i], info}
                        );
                }
            }
        }

        private void SetExitCode(int exitCode)
        {
            if (_exitCode != exitCode)
            {
                _exitCode = exitCode;
                System.Environment.ExitCode = exitCode;
            }
        }

        private object ShutdownCallback(object arg)
        {
            ShutdownImpl();
            return null;
        }
        /// <summary>
        /// This method gets called on dispatch of the Shutdown DispatcherOperationCallback
        /// </summary>
        private void ShutdownImpl()
        {
            // Event handler exception continuality: if exception occurs in Exit event handler,
            // our cleanup action is to finish Shutdown since Exit cannot be cancelled. We don't
            // want user to use throw exception and catch it to cancel Shutdown.
            try
            {
                DoShutdown();
            }
            finally
            {
                // Quit the dispatcher if we ran our own.
                if (_ownDispatcherStarted == true)
                {
                    Dispatcher.CriticalInvokeShutdown();
                }

                ServiceProvider = null;
            }
        }

        private static bool IsValidShutdownMode(ShutdownMode value)
        {
            return value == ShutdownMode.OnExplicitShutdown
                || value == ShutdownMode.OnLastWindowClose
                || value == ShutdownMode.OnMainWindowClose;
        }

        private void OnPreBPReady(object sender, BPReadyEventArgs e)
        {
            NavService.PreBPReady -= new BPReadyEventHandler(OnPreBPReady);
            NavService.AllowWindowNavigation = false;

            ConfigAppWindowAndRootElement(e.Content, e.Uri);

            NavService = null;
            e.Cancel = true;
        }

        private void ConfigAppWindowAndRootElement(object root, Uri uri)
        {
            Window w = root as Window;
            if (w == null)
            {
                //Creates and returns a NavigationWindow for standalone cases
                //For browser hosted cases, returns the RootBrowserWindow precreated by docobjhost
                NavigationWindow appWin = GetAppWindow();

                //Since we cancel PreBPReady event here, the other navigation events won't fire twice.
                appWin.Navigate(root, new NavigateInfo(uri));

                // To avoid flash and re-layout, call Window.Show() asynchronously, at Normal priority, which
                // will happen right after navigation to the content completes.
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new SendOrPostCallback((window) =>
                    {
                        if (!((Window)window).IsDisposed)
                        {
                            ((Window)window).Show();
                        }
                    }), appWin);
            }
            else
            {
                // if Visibility has not been set, we set it to true
                // Also check whether the window is already closed when we get here - applications could close the window
                // in its constructor.
                if (!w.IsVisibilitySet && !w.IsDisposed)
                {
                    w.Visibility = Visibility.Visible;
                }
            }
        }


        /// <summary>
        /// Plays a system sound using the PlaySound api.  This is a managed equivalent of the
        /// internet explorer method IEPlaySoundEx() from ieplaysound.cpp.
        /// </summary>
        /// <param name="soundName">The name of the sound to play</param>
        /// <returns>true if a sound was successfully played</returns>
        private void PlaySound(string soundName)
        {
            string soundFile = GetSystemSound(soundName);

            if (!string.IsNullOrEmpty(soundFile))
            {
                UnsafeNativeMethods.PlaySound(soundFile, IntPtr.Zero, PLAYSOUND_FLAGS);
            }
        }

        private string GetSystemSound(string soundName)
        {
            string soundFile = null;
            string regPath = string.Format(CultureInfo.InvariantCulture, SYSTEM_SOUNDS_REGISTRY_LOCATION, soundName);
            try
            {
                using (RegistryKey soundKey = Registry.CurrentUser.OpenSubKey(regPath))
                {
                    if (soundKey != null)
                    {
                        soundFile = (string)(soundKey.GetValue(""));
                    }
                }
            }
            // When the value of the register key is empty, the IndexOutofRangeException is thrown.
            // (Application.PlaySourd crash when the registry is broken)
            catch (System.IndexOutOfRangeException)
            {
            }

            return soundFile;
        }

        private EventHandlerList Events
        {
            get
            {
                if (_events == null)
                {
                    _events = new EventHandlerList();
                }
                return _events;
            }
        }


        //
        // Check if the current Uri is for the root element in a baml stream which is processed by an
        // outer LoadBaml.  such as it is through Navigate(uri) or LoadComoponent(uri).
        //
        private static bool IsComponentBeingLoadedFromOuterLoadBaml(Uri curComponentUri)
        {
            bool isRootElement = false;

            Invariant.Assert(curComponentUri != null, "curComponentUri should not be null");

            if (s_NestedBamlLoadInfo != null && s_NestedBamlLoadInfo.Count > 0)
            {
                //
                // Get the top LoadBamlSynInfo from the stack.
                //
                NestedBamlLoadInfo loadBamlSyncInfo = s_NestedBamlLoadInfo.Peek() as NestedBamlLoadInfo;

                if (loadBamlSyncInfo != null && loadBamlSyncInfo.BamlUri != null &&
                    loadBamlSyncInfo.BamlStream != null &&
                    BindUriHelper.DoSchemeAndHostMatch(loadBamlSyncInfo.BamlUri, curComponentUri))
                {
                    string fileInBamlConvert = loadBamlSyncInfo.BamlUri.LocalPath;
                    string fileCurrent = curComponentUri.LocalPath;

                    Invariant.Assert(fileInBamlConvert != null, "fileInBamlConvert should not be null");
                    Invariant.Assert(fileCurrent != null, "fileCurrent should not be null");

                    if (String.Compare(fileInBamlConvert, fileCurrent, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        //
                        // This is the root element of the xaml page which is being loaded to creat a tree
                        // through LoadBaml call by BamlConverter.
                        //

                        isRootElement = true;
                    }
                    else
                    {
                        // We consider Pack://application,,,/page1.xaml refers to the same component as
                        // Pack://application,,,/myapp;Component/page1.xaml.
                        string[] bamlConvertUriSegments = fileInBamlConvert.Split(new Char[] { '/', '\\' });
                        string[] curUriSegments = fileCurrent.Split(new Char[] { '/', '\\' });

                        int l = bamlConvertUriSegments.Length;
                        int m = curUriSegments.Length;

                        // The length of the segments should be at least 1, because the first one is empty.
                        Invariant.Assert((l >= 2) && (m >= 2));

                        int diff = l - m;
                        // The segment length can only be different in one for myapp;Component
                        if (Math.Abs(diff) == 1)
                        {
                            // Check whether the file name is the same.
                            if (String.Compare(bamlConvertUriSegments[l - 1], curUriSegments[m - 1], StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                string component = (diff == 1) ? bamlConvertUriSegments[1] : curUriSegments[1];

                                isRootElement = BaseUriHelper.IsComponentEntryAssembly(component);
                            }
                        }
                    }
                }
            }

            return isRootElement;
        }


        private object RunDispatcher(object ignore)
        {
            if (_ownDispatcherStarted)
            {
                throw new InvalidOperationException(SR.Get(SRID.ApplicationAlreadyRunning));
            }
            _ownDispatcherStarted = true;
            Dispatcher.Run();
            return null;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        static private object                           _globalLock;
        static private bool                             _isShuttingDown;
        static private bool                             _appCreatedInThisAppDomain;
        static private Application                      _appInstance;
        static private Assembly                         _resourceAssembly;

        // Keep LoadBamlSyncInfo stack so that the Outer LoadBaml and Inner LoadBaml( ) for the same
        // Uri share the related information.
        [ThreadStatic]
        private static Stack<NestedBamlLoadInfo> s_NestedBamlLoadInfo = null;

        private Uri                         _startupUri;
        private Uri                         _applicationMarkupBaseUri;
        private HybridDictionary            _htProps;
        private WindowCollection            _appWindowList;
        private WindowCollection            _nonAppWindowList;
        private Window                      _mainWindow;
        private ResourceDictionary          _resources;

        private bool                        _ownDispatcherStarted;
        private NavigationService           _navService;

        private SecurityCriticalDataForSet<MimeType> _appMimeType;
        private IServiceProvider            _serviceProvider;

        private bool                        _appIsShutdown;
        private int                         _exitCode;

        private ShutdownMode                _shutdownMode = ShutdownMode.OnLastWindowClose;

        private HwndWrapper                 _parkingHwnd;

        private HwndWrapperHook             _appFilterHook;

        private EventHandlerList            _events;
        private bool                        _hasImplicitStylesInResources;

        private static readonly object EVENT_STARTUP = new object();
        private static readonly object EVENT_EXIT = new object();
        private static readonly object EVENT_SESSIONENDING = new object();

        private const SafeNativeMethods.PlaySoundFlags PLAYSOUND_FLAGS = SafeNativeMethods.PlaySoundFlags.SND_FILENAME |
                                                                            SafeNativeMethods.PlaySoundFlags.SND_NODEFAULT |
                                                                            SafeNativeMethods.PlaySoundFlags.SND_ASYNC |
                                                                            SafeNativeMethods.PlaySoundFlags.SND_NOSTOP;
        private const string SYSTEM_SOUNDS_REGISTRY_LOCATION            = @"AppEvents\Schemes\Apps\Explorer\{0}\.current\";
        private const string SYSTEM_SOUNDS_REGISTRY_BASE                = @"HKEY_CURRENT_USER\AppEvents\Schemes\Apps\Explorer\";
        private const string SOUND_NAVIGATING                           = "Navigating";
        private const string SOUND_COMPLETE_NAVIGATION                  = "ActivatingDocument";

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------
        #region NavigationStateChange
        internal enum NavigationStateChange : byte
        {
            Navigating,
            Completed,
            Stopped,
        }
        #endregion NavigationStateChange
    }

    #endregion Application Class


    //
    // In Navigation(uri) and LoadComponent(uri), below scenarios might occur:
    //
    //   After a baml stream  is passed into LoadBaml( ) call, when instance
    //   of the root element is created, it would call the generated InitializeComponent( )
    //   which then calls LoadBaml( ) with the baml stream created from the same uri again.
    //
    // The LoadBaml( ) triggered by Navigation or LoadComponent(uri) is named as Outer LoadBaml.
    // The LoadBaml( ) called by IC in ctor of root Element is named as Inner LoadBaml.
    //
    // To prevent the baml stream created from the same Uri from being loaded twice, we need
    // a way to detect whether the Outer LoadBaml and Inner LoadBaml share the same share the
    // same stream and the same parser context.
    //
    internal class NestedBamlLoadInfo
    {
        //
        // ctor of NestedBamlLoadInfo
        //
        internal NestedBamlLoadInfo(Uri uri, Stream stream, bool bSkipJournalProperty)
        {
            _BamlUri = uri;
            _BamlStream = stream;
            _SkipJournaledProperties = bSkipJournalProperty;
        }

        #region internal properties

        //
        // OuterBamlUri property
        //
        internal Uri BamlUri
        {
            get { return _BamlUri;  }
            set { _BamlUri = value; }   // Code could reset the OuterBamlUri for performance optimization.
        }

        //
        // OuterBamlStream property
        //
        internal Stream BamlStream
        {
            get { return _BamlStream; }
        }

        //
        // OuterSkipJournaledProperties
        //
        internal bool SkipJournaledProperties
        {
            get { return _SkipJournaledProperties; }
        }

        #endregion


        #region private field

        // Keep Uri which is being handled by Outer LoadBaml in this thread.
        private Uri _BamlUri = null;

        // Keep the stream which is being handled by Outer LoadBaml for above Uri in this thread.
        private Stream _BamlStream = null;

        // Whether or not SkipJournalProperty when a baml stream is handled in Outer LoadBaml.
        private bool _SkipJournaledProperties = false;

        #endregion
    }

    #region enum ShutdownMode

    /// <summary>
    ///     Enum for ShutdownMode
    /// </summary>
    public enum ShutdownMode : byte
    {
        /// <summary>
        ///
        /// </summary>
        OnLastWindowClose = 0,

        /// <summary>
        ///
        /// </summary>
        OnMainWindowClose = 1,

        /// <summary>
        ///
        /// </summary>
        OnExplicitShutdown

        // NOTE: if you add or remove any values in this enum, be sure to update Application.IsValidShutdownMode()
    }

    #endregion enum ShutdownMode

    #region enum ReasonSessionEnding

    /// <summary>
    ///     Enum for ReasonSessionEnding
    /// </summary>
    public enum ReasonSessionEnding : byte
    {
        /// <summary>
        ///
        /// </summary>
        Logoff = 0,
        /// <summary>
        ///
        /// </summary>
        Shutdown
    }
    #endregion enum ReasonSessionEnding
}

