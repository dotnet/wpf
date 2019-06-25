// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//
//
//
// Description: This is an internal proxy wrapper class over Application class.
//              This derives from MarshalByRefObject and thus facilitates calling
//              the AppCode from DocobjHost that may be running in a different
//              AppDomain.  The other thing it allows is to be able to create
//              the App Object on the right thread by exposing a delegate that
//              could be set to point to the code that creates the AppObject and
//              invoking the delegate from the right thread.
//
//
//

//------------------------------------------------------------------------------

//**
//** IMPORTANT: Running arbitrary application code in the context of an incoming call from the browser
//**    should be avoided. This could lead to unexpected reentrancy (on either side) or making the
//      browser frame unresponsive while the application code is running. Bug 1139336 illustrates
//      what can happen if the application code enters a local message loop while the browser is
//      blocked. To avoid such situations in general, use Dispatcher.BeginInvoke() instead of making
//      direct calls into unknown code.


using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Threading;

using MS.Internal;
using MS.Internal.Documents;
using MS.Internal.Documents.Application;
using MS.Internal.IO.Packaging;
using MS.Internal.IO.Packaging.CompoundFile;
using MS.Internal.PresentationFramework;
using MS.Internal.Utility;
using MS.Internal.AppModel;
using MS.Utility;

//In order to avoid generating warnings about unknown message numbers and
//unknown pragmas when compiling your C# source code with the actual C# compiler,
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace MS.Internal.AppModel
{
    // All security sensitive classes should be sealed or protected with InheritanceDemand
    internal sealed class ApplicationProxyInternal : MarshalByRefObject
    {
        [Serializable]
        internal class InitData
        {
            internal IServiceProvider ServiceProvider;
            
            internal IHostBrowser HostBrowser;
            internal SecurityCriticalDataForSet<MimeType> MimeType;
            internal SecurityCriticalDataForSet<Uri> ActivationUri;
            internal string Fragment;
            internal object UcomLoadIStream;
            internal bool HandleHistoryLoad;
            internal string UserAgentString;
            internal HostingFlags HostingFlags;
            internal Rect WindowRect;
            internal bool ShowWindow;
        };

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors

        /// <summary>
        /// ApplicationProxyInternal is created only for browser-hosted applications.
        /// </summary>
        internal ApplicationProxyInternal()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_AppProxyCtor);

            if (_proxyInstance != null)
            {
                throw new InvalidOperationException(SR.Get(SRID.MultiSingleton, this.GetType().FullName));
            }
            // Set this here so it will be true for documents or applications (i.e. anything in the browser.)
            BrowserInteropHelper.SetBrowserHosted(true);
            _proxyInstance = this;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods

        public override object InitializeLifetimeService()
        {
            //Keeps it alive until the AppDomain is teared down which is what we want.
            //Otherwise the .Net remoting infrastructure releases all remote objects in 5 mins
            //if there are no sponsors registered with the lease manager for the remote object.
            //This is an alternative to the client side registering a sponsor by the server object
            //marking itself to be kept alive for the life of the AppDomain.
            return null;
        }

        //Creates the internal RootBrowserWindow. If the startup Uri points
        //to a Window/NavigationWindow, we still need to create this empty
        //RootBrowserWindow so we can repaint properly inside the browser window

        internal void CreateRootBrowserWindow()
        {
            if (_rbw.Value == null)
            {

                Application.Current.Dispatcher.Invoke(
                                    DispatcherPriority.Send,
                                    new DispatcherOperationCallback(_CreateRootBrowserWindowCallback),
                                    null);
            }
        }

        internal bool FocusedElementWantsBackspace()
        {
            TextBoxBase textBoxBase = Keyboard.FocusedElement as TextBoxBase;
            if (textBoxBase != null)
            {
                return true; // textBoxBase.IsEmpty
            }

            PasswordBox passwordBox = Keyboard.FocusedElement as PasswordBox;
            if (passwordBox != null)
            {
                return true; // passwordBox.IsEmpty
            }

            return false;
        }

        private object _CreateRootBrowserWindowCallback(object unused)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_RootBrowserWindowSetupStart);

            RootBrowserWindow = RootBrowserWindow.CreateAndInitialize();

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_RootBrowserWindowSetupEnd);

            return null ;
        }

        // Calls the Run method on the app object.
        internal int Run(InitData initData)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_AppProxyRunStart);

            // Keep in mind that Run() is called once in the default AppDomain and then in the XBAP's domain.
            // We want initialization of statics to happen in both AppDomains.

            if (!AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                // Since IHostBrowser was marshaled from the default AppDomain as a managed interface, we get
                // a Remoting transparent proxy here. Any calls on the interface would have to first be marshaled
                // to the default AppDomain, where the CLR will realize it's actually a COM interface. This is 
                // wasteful since the object lives in the browser process. So, to shake off the Remoting layer,
                // we round-trip the IHostBrowser reference through IUknown/IntPtr.
                IntPtr pObj = Marshal.GetIUnknownForObject(initData.HostBrowser);
                try { initData.HostBrowser = (IHostBrowser)Marshal.GetObjectForIUnknown(pObj); }
                finally { Marshal.Release(pObj); }
            }
            BrowserInteropHelper.HostBrowser = initData.HostBrowser;

            MimeType mimeType = initData.MimeType.Value;
            _mimeType.Value = mimeType;
            Uri = initData.ActivationUri.Value;
            WpfWebRequestHelper.DefaultUserAgent = initData.UserAgentString;
            BrowserInteropHelper.HostingFlags = initData.HostingFlags;

            // These methods are asynchronous.
            // If the RootBrowserWindow is not created yet, only the size for it will be stored.
            Move(initData.WindowRect);
            Show(initData.ShowWindow);

            switch (mimeType)
            {
                case MimeType.Markup:
                    // Make a dummy application (in lieu of the one provided by the defunct XamlViewer.xbap).
                    Invariant.Assert(AppDomain.CurrentDomain.FriendlyName == "XamlViewer");
                    Application app = new Application();
                    app.StartupUri = Uri;
                    // Any URL #fragment is appended to StartupUri in _RunDelegate().
                    // For history navigation, ApplicationProxyInternal has already started navigation to the
                    // last journal entry captured. (This journal entry may include a #fragment target and/or
                    // a CustomContentState.)
                    break;

                case MimeType.Application:
                    //This is a browser app, the application object has already been created
                    break;

                case MimeType.Document:
                    throw new NotImplementedException(); // removed in v4
                case MimeType.Unknown:
                default:
                    throw new InvalidOperationException();
            }

            // Set the Application.MimeType
            // Since loading containers causes the application to be constructed now,
            // the initial setting of the MimeType does not get passed to the application.
            Application.Current.MimeType = mimeType;
            ServiceProvider = initData.ServiceProvider; // also sets Application.ServiceProvider

            Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Send,
                new DispatcherOperationCallback(_RunDelegate),
                initData);

            int exitCode = Application.Current.RunInternal(null);

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_AppProxyRunEnd);

            return exitCode;
        }

        private object _RunDelegate( object args )
        {
            InitData initData = (InitData)args;

            Application currentApp = Application.Current;
            if (currentApp != null && !(currentApp is XappLauncherApp))
            {
                string fragment = initData.Fragment;
                if (!String.IsNullOrEmpty(fragment) && currentApp.StartupUri != null)
                {
                    // Apply Fragment to Application StartupUri.
                    UriBuilder uriBuilder;
                    Uri absUri = currentApp.StartupUri;

                    if (currentApp.StartupUri.IsAbsoluteUri == false)
                    {
                        absUri = new Uri(BindUriHelper.BaseUri, currentApp.StartupUri);
                    }

                    uriBuilder = new UriBuilder(absUri);
                    if (fragment.StartsWith(FRAGMENT_MARKER, StringComparison.Ordinal))
                    {
                        fragment = fragment.Substring(FRAGMENT_MARKER.Length);
                    }
                    uriBuilder.Fragment = fragment;
                    currentApp.StartupUri = uriBuilder.Uri;
                }

                CreateRootBrowserWindow();
            }

            //If we were started through IPersistHistory::Load, load from the history stream instead
            //of navigating to the StartupPage
            if (initData.UcomLoadIStream != null && initData.HandleHistoryLoad)
            {
                LoadHistoryStream(DocObjHost.ExtractComStream(initData.UcomLoadIStream), /*firstHistoryLoad=s*/true);
            }
            return null;
        }

        // Show or hide view.
        internal void Show(bool show)
        {
            _show = show;
            if (Application.Current != null && RootBrowserWindow != null)
            {
                Application.Current.Dispatcher.BeginInvoke(
                                    DispatcherPriority.Send,
                                    new DispatcherOperationCallback(_ShowDelegate),
                                    null);
            }

        }

        private object _ShowDelegate(object ignore)
        {
            // The RBW might be torn down just before the DispatcherOperation is invoked.
            if (RootBrowserWindow == null || Application.IsShuttingDown)
                return null;

            if (_show)
            {
                // The window is shown asynchronously (using Visibility, not Show()) to allow first restoring
                // the Journal on history navigation. This prevents bug 1367999.
                _rbw.Value.Visibility = Visibility.Visible;

                // initial focus should be on us, not the browser frame
                // Focusing is done asynchronously because Visibility actually changes asynchronously.
                Application.Current.Dispatcher.BeginInvoke(
                    // same priority as used in the Window.Visibility PropertyChangedCallback
                    DispatcherPriority.Normal,
                    new DispatcherOperationCallback(_FocusDelegate), null);
            }
            else
            {
                _rbw.Value.Visibility = Visibility.Hidden;
            }

            return null;
        }

        private object _FocusDelegate(object unused)
        {
            if (_rbw.Value != null)
            {
                try
                {
                    MS.Win32.UnsafeNativeMethods.SetFocus(new HandleRef(_rbw.Value, _rbw.Value.CriticalHandle));
                }
#pragma warning disable 6502
                // The browser may temporarily disable the RBW. Then SetFocus() fails.
                // This is known to happen when the browser pops up the modal dialog about the Information Bar.
                catch (System.ComponentModel.Win32Exception)
                {
                    Debug.WriteLine("SetFocus() on RootBrowserWindow failed.");
                }
#pragma warning restore 6502
            }
            return null;
        }

        internal void Move(Rect windowRect)
        {
            if (Application.Current != null && RootBrowserWindow != null)
            {
                Application.Current.Dispatcher.BeginInvoke(
                                    DispatcherPriority.Send,
                                    new DispatcherOperationCallback(_MoveDelegate),
                                    windowRect);
            }
            else
            {
                // We got UIActivated too early.  Remember the data passed in.
                _windowRect = windowRect;
                _rectset = true;
            }
        }

        private object _MoveDelegate( object moveArgs )
        {
            // The RBW might be closed just before _MoveDelegate() is called. => check _rbw again.
            if (_rbw.Value != null && !Application.IsShuttingDown)
            {
                Rect r = (Rect)moveArgs;

                // ResizeMove is implemented by RBW and should be called here
                // since it resizes and moves the WS_CHILD window.  Do not call
                // Height/Width & Top/Left here since they govern the browser
                // window properties.
                _rbw.Value.ResizeMove((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
            }
            return null;
        }

        // Shutdown the App.
        // Note: The "post" in the method name is legacy. Now all of Application's shutdown work is complete
        // when this method returns. In particular, the managed Dispatcher is shut down.
        internal void PostShutdown()
        {
            Cleanup();
            _proxyInstance = null;

            Application app = Application.Current;
            if (app != null)
            {
                XappLauncherApp launcherApp = app as XappLauncherApp;
                if (launcherApp != null)
                {
                    launcherApp.AbortActivation();
                    Debug.Assert(Application.IsShuttingDown);
                }
                else
                {
                    //this calls into the internal helper and is hardcoded for a clean
                    // shutdown
                    app.CriticalShutdown(0);

                    // The Application.Exit event is raised in a Dispatcher callback at Normal priority.
                    // Blocking on this callback here ensures that the event will be raised before we've
                    // disconnected from the browser. An XBAP may want, in particular, to write a cookie.
                    app.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new DispatcherOperationCallback(delegate(object unused) { return null; }), null);
                }
            }
        }

        //
        // Activate or Deactivate RootBrowserWindow
        //

        internal void Activate(bool fActivate)
        {
            if (Application.Current != null && RootBrowserWindow != null)
            {
                Application.Current.Dispatcher.BeginInvoke(
                                    DispatcherPriority.Send,
                                    new DispatcherOperationCallback(_ActivateDelegate),
                                    fActivate);
            }
        }

        private object _ActivateDelegate(object arg )
        {
            if (RootBrowserWindow != null)
            {
                bool fActivate = (bool)arg;

                _rbw.Value.HandleActivate(fActivate);
                if (fActivate)
                {
                    _FocusDelegate(null);
                }
            }
            return null;
        }

        internal bool CanInvokeJournalEntry(int entryId)
        {
            if (Application.Current == null)
            {
                return false;
            }

            return (bool)Application.Current.Dispatcher.Invoke(
            DispatcherPriority.Send,
                (DispatcherOperationCallback) delegate(object unused)
            {
                NavigationWindow window = Application.Current.MainWindow as NavigationWindow;

                if (window == null)
                    return false;

                return window.JournalNavigationScope.CanInvokeJournalEntry(entryId);
            },
            null);
        }

        // Private class just to facilitate passing of data back to GetSaveHistoryBytes.
        private class SaveHistoryReturnInfo
        {
            internal string uri;
            internal string title;
            internal int entryId;
            internal byte[] saveByteArray ;
        }

        /// <summary> Called by the browser to serialize the entire journal or just the index of
        /// the current entry. The second case is when an internal Journal update needs to be
        /// reflected in the TravelLog.
        /// </summary>
        /// <param name="arg"> true is the entire Journal is to serialized </param>
        private object _GetSaveHistoryBytesDelegate(object arg)
        {
            bool entireJournal = (bool)arg;

            SaveHistoryReturnInfo info = new SaveHistoryReturnInfo();

            // DevDiv 716414 / DevDiv2 196517 & 224724:
            // Checking _serviceProvider for null due to COM reentrancy issues observed by customers.
            // Users who perform frequent refreshes may experience shutdown while we are still starting up and are not fully initialized.
            // The ServiceProvider field is one of the last things to be set during initialization, so if this is null
            // we know that we have not finished initialization much less run the app and thus have no need to save history.
            if (_serviceProvider == null)
                return null;

            // When we are here, the browser has just started to shut down, so we should only check
            // whether the application object is shutting down.
            if (Application.IsApplicationObjectShuttingDown == true)
                return null;

            Invariant.Assert(_rbw.Value != null, "BrowserJournalingError: _rbw should not be null");

            Journal journal = _rbw.Value.Journal;
            Invariant.Assert(journal != null, "BrowserJournalingError: Could not get internal journal for the window");

            JournalEntry entry;
            if (entireJournal) // The application is about to be shut down...
            {
                NavigationService topNavSvc = _rbw.Value.NavigationService;
                try
                {
                    topNavSvc.RequestCustomContentStateOnAppShutdown();
                }
                catch(Exception e)
                {
                    if(CriticalExceptions.IsCriticalException(e))
                    {
                        throw;
                    }
                }

                journal.PruneKeepAliveEntries();

                // Since the current page is not added to the journal until it is replaced,
                // we add it here explicitly to the internal Journal before serializing it.
                entry = topNavSvc.MakeJournalEntry(JournalReason.NewContentNavigation);
                if (entry != null && !entry.IsAlive())
                {
                    if (entry.JEGroupState.JournalDataStreams != null)
                    {
                        entry.JEGroupState.JournalDataStreams.PrepareForSerialization();
                    }
                    journal.UpdateCurrentEntry(entry);
                }
                else // Maybe the current content is null or a PageFunction doesn't want to be journaled.
                {   // Then the previous navigable page, if any, should be remembered as current.
                    entry = journal.GetGoBackEntry();
                    // i. _LoadHistoryStreamDelegate() has a similar special case.
                }
            }
            else
            {   // (Brittle) Assumption: GetSaveHistoryBytes() is called after the current entry has
                // been updated in the internal journal but before the new navigation is committed.
                // This means journal.CurrentEntry is what was just added (or updated).
                // Note that it would be wrong to call topNavSvc.MakeJournalEntry() in this case because
                // the navigation that just took place may be in a different NavigationService (in a
                // frame), and here we don't know which one it is.
                entry = journal.CurrentEntry;

                // The entry may be null here when the user has selected "New Window" or pressed Ctrl+N.
                // In this case the browser calls us on IPersistHistory::Save and then throws that data
                // away.  Hopefully at some point in the future that saved data will be loaded in the new
                // window via IPersistHistory::Load.  This unusual behavior is tracked in bug 1353584.
            }

            if (entry != null)
            {
                info.title = entry.Name;
                info.entryId = entry.Id;
            }
            else
            {
                info.title = _rbw.Value.Title;
            }

            // We only use the base URI here because the travel log will validate a file URI when making a PIDL.
            // We use the URI stored in the JournalEntry, and the travel log doesn't care what the URI is, so
            // duplicates don't matter.
            info.uri = BindUriHelper.UriToString(Uri);

            MemoryStream saveStream = new MemoryStream();

            saveStream.Seek(0, SeekOrigin.Begin);

            if (entireJournal)
            {
                //Save the Journal and BaseUri also. We don't need BaseUri except for the xaml case
                //since this is set specially for the container case (ssres scheme). Exe case
                //will pretty much set it to the exe path. For the xaml case it is set to the path
                //of the first uri(eg BaseDir\page1.xaml) that was navigated to.
                //BaseDir/Subdir/page2.xaml is also considered to be in the same extent and when
                //we navigate back to the app from a webpage, the baseUri should still be BaseDir
                //not BaseDir/Subdir. We were setting the BaseDir from JournalEntry.Uri but we may
                //end up setting BaseUri to BaseDir/Subdir which is not the same. So explicitly
                //persist BaseUri as well
                BrowserJournal browserJournal = new BrowserJournal(journal, BindUriHelper.BaseUri);

                try
                {
                    saveStream.WriteByte(BrowserJournalHeader);
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(saveStream, browserJournal);
                }
                catch(Exception e)
                {
                    if(CriticalExceptions.IsCriticalException(e))
                    {
                        throw;
                    }

                    // The application is shutting down and the exception would not be reported anyway.
                    // This is here to help with debugging and failure analysis.
                    Invariant.Assert(false, "Failed to serialize the navigation journal: " + e);
                }
            }
            else
            {
                saveStream.WriteByte(JournalIdHeader);
                WriteInt32(saveStream, info.entryId);
            }

            info.saveByteArray = saveStream.ToArray();
            ((IDisposable)saveStream).Dispose();

            return info ;
        }


        internal byte[] GetSaveHistoryBytes(bool persistEntireJournal, out int journalEntryId, out string uriString, out string titleString)
        {
            SaveHistoryReturnInfo info = null ;

            if (Application.Current != null)
            {
                info = ( SaveHistoryReturnInfo) Application.Current.Dispatcher.Invoke(
                                DispatcherPriority.Send,
                                new DispatcherOperationCallback(_GetSaveHistoryBytesDelegate) ,
                                persistEntireJournal);
            }

            if ( info != null )
            {
                journalEntryId = info.entryId;
                uriString = info.uri;
                titleString = info.title;
                return info.saveByteArray;
            }
            else
            {
                journalEntryId = 0;
                uriString = null;
                titleString = null;
                return null;
            }
        }

        internal void LoadHistoryStream(MemoryStream loadStream, bool firstLoadFromHistory)
        {
            if (Application.Current == null)
            {
                return;
            }

            LoadHistoryStreamInfo info = new LoadHistoryStreamInfo();
            info.loadStream = loadStream ;
            info.firstLoadFromHistory = firstLoadFromHistory ;

            Application.Current.Dispatcher.Invoke(
                        DispatcherPriority.Send,
                        new DispatcherOperationCallback(_LoadHistoryStreamDelegate),
                        info);
        }

        private class LoadHistoryStreamInfo
        {
            internal MemoryStream loadStream ;
            internal bool firstLoadFromHistory;
        }

        private object _LoadHistoryStreamDelegate( object arg )
        {
            Journal             journal     = null;
            JournalEntry        entry       = null;

            LoadHistoryStreamInfo info = (LoadHistoryStreamInfo) arg ;

            if (IsShutdown() == true)
                return null;

            // Reset the memory stream pointer back to the begining and get the persisted object
            info.loadStream.Seek(0, System.IO.SeekOrigin.Begin);

            object journaledObject = DeserializeJournaledObject(info.loadStream);

            //This is the very first load from history, so need to set the BaseUri and StartupUri.
            if (info.firstLoadFromHistory)
            {
                // The journal does not get saved on Ctrl+N. Because of this,
                // here we can get just an index, like in the 'else' case below.
                if(!(journaledObject is BrowserJournal))
                    return null;

                BrowserJournal browserJournal = (BrowserJournal)journaledObject;

                journal = browserJournal.Journal;
                entry = journal.CurrentEntry;
                if (entry == null) // See special case in _GetSaveHistoryBytesDelegate().
                {
                    entry = journal.GetGoBackEntry(); // could still be null
                }

                //This will create the frame to use for hosting
                {
                    NavigationService navigationService = null;
                    navigationService = _rbw.Value.NavigationService;
                }
                _rbw.Value.SetJournalForBrowserInterop(journal);

                //This should already be set for the container and exe cases. The former
                //sets it to the transformed ssres scheme and we don't want to overwrite it.
                if (BindUriHelper.BaseUri == null)
                {
                    BindUriHelper.BaseUri = browserJournal.BaseUri;
                }

                //CHECK: For xaml case, what should we set as the Startup Uri ? We set it the initial
                //uri we started with, should this be changed to creating the window explicitly
                //and navigating the window instead of setting the StartupUri?
                //(Application.Current as Application).StartupUri = entry.Uri;

                Debug.Assert(Application.Current != null, "BrowserJournalingError: Application object should already be created");

                if (entry != null)
                {
                    //Prevent navigations to StartupUri for history loads by canceling the StartingUp event
                    Application.Current.Startup += new System.Windows.StartupEventHandler(this.OnStartup);
                    
                    _rbw.Value.JournalNavigationScope.NavigateToEntry(entry);
                }
                //else: fall back on navigating to StartupUri
            }
            else
            {
                if(!(journaledObject is int))
                    return null;

                journal = _rbw.Value.Journal;

                int index = journal.FindIndexForEntryWithId((int)journaledObject);

                Debug.Assert(journal[index].Id == (int)journaledObject, "BrowserJournalingError: Index retrieved from journal stream does not match index of journal entry");

                // Check whether the navigation is canceled.
                if (! _rbw.Value.JournalNavigationScope.NavigateToEntry(index))
                {
                    // When the navigation is canceled, we want to notify browser to prevent the internal journal from
                    // getting out of sync with the browser's.
                    // The exception will be caught by the interop layer and browser will cancel the navigation as a result.

                    // If the navigation is initiated pragmatically by calling GoBack/Forward (comparing to user initiated
                    // by clicking the back/forward button),  this will result in a managed exception at the call to ibcs.GoBack()
                    // in rbw.GoBackOverride(). rbw catches the exception when this happens.

                    throw new OperationCanceledException();
                }
            }

            return null;
        }

        private object DeserializeJournaledObject(MemoryStream inputStream)
        {
            object deserialized = null;
            
            int header = inputStream.ReadByte();
            if (header >= 0)
            {
                switch((byte)header)
                {
                    case JournalIdHeader : 
                    {
                        deserialized = ReadInt32(inputStream);
                        break;
                    }
                    
                    case BrowserJournalHeader:
                    {
                        try
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            deserialized = formatter.Deserialize(inputStream);
                        }
                        catch (SecurityException)
                        {
                            deserialized = null;
                        }
                        
                        break;
                    }
                    
                    default:
                    {
                        throw new FormatException();
                    }
                }
            }
            
            return deserialized;
        }

        // See if an App instance is currently loaded.
        internal bool IsAppLoaded()
        {
            return (Application.Current == null ? false : true);
        }

        // Return the internal static variable _shutdown.
        internal bool IsShutdown()
        {
            return Application.IsShuttingDown;
        }

        internal void Cleanup()
        {
            if (Application.Current != null)
            {
                IBrowserCallbackServices bcs = Application.Current.BrowserCallbackServices;
                if (bcs != null)
                {
                    Debug.Assert(!Application.IsApplicationObjectShuttingDown);
                    // Marshal.ReleaseComObject(bcs) has to be called so that the refcount of the
                    // native objects goes to zero for clean shutdown. But it should not be called
                    // right away, because there may still be DispatcherOperations in the queue
                    // that will attempt to use IBCS, especially during downloading/activation.
                    // Last, it can't be called with prioroty lower than Normal, because that's
                    // the priority of Applicatoin.ShudownCallback(), which shuts down the
                    // Dispatcher.
                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal, new DispatcherOperationCallback(ReleaseBrowserCallback), bcs);
                }
            }

            ServiceProvider = null;
            ClearRootBrowserWindow();

            if (_storageRoot != null && _storageRoot.Value != null  )
            {
                _storageRoot.Value.Close();
            }

            // Due to the dependecies the following objects have to be released
            // in the following order: _document, DocumentManager,
            // _packageStream, _unmanagedStream.

            if (_document.Value is PackageDocument)
            {
                // We are about to close the package ad remove it from the Preloaded Packages Store.
                // Let's make sure that the data structures are consistent. The package that we hold is
                // actually in the store under the URI that we think it should be using
                Debug.Assert(((PackageDocument)_document.Value).Package ==
                                    PreloadedPackages.GetPackage(PackUriHelper.GetPackageUri(PackUriHelper.Create(Uri))));

                // We need to remove the Package from the PreloadedPackage storage,
                // so that potential future requests would fail in a way of returning a null (resource not found)
                // rather then return a Package or stream that is already Closed
                PreloadedPackages.RemovePackage(PackUriHelper.GetPackageUri(PackUriHelper.Create(Uri)));

                ((PackageDocument)_document.Value).Dispose();
                _document.Value = null;
            }

            if (_mimeType.Value == MimeType.Document)
            {
                DocumentManager.CleanUp();
            }

            if (_packageStream.Value != null)
            {
                _packageStream.Value.Close();
            }

            if (_unmanagedStream.Value != null)
            {
                Marshal.ReleaseComObject(_unmanagedStream.Value);
                _unmanagedStream = new SecurityCriticalData<object>(null);
            }
        }

        private object ReleaseBrowserCallback(object browserCallback)
        {
            Marshal.ReleaseComObject(browserCallback);
            BrowserInteropHelper.ReleaseBrowserInterfaces();
            return null;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        #region Internal Properties

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal RootBrowserWindowProxy RootBrowserWindowProxy
        {
            get
            {
                if (_rbwProxy.Value == null)
                {
                    CreateRootBrowserWindow();
                }
                return _rbwProxy.Value;
            }
        }

        internal RootBrowserWindow RootBrowserWindow
        {
            get
            {
                return _rbw.Value;
            }
            private set
            {
                _rbw.Value = value;
                if (value == null)
                {
                    _rbwProxy.Value = null;
                }
                else
                {
                    _rbwProxy.Value = new RootBrowserWindowProxy(value);

                    if (_rectset == true)
                    {
                        // If UIActivation already happened, set the size with
                        // cached values.
                        Move(_windowRect);
                        _rectset = false;
                    }

                    //Incase Show() was called before we had a chance to create the window
                    //If the old and new values are the same, this will be a no-op anyway
                    Show(_show);
                }
            }
        }

        internal bool RootBrowserWindowCreated { get { return _rbw.Value != null; } }

        internal OleCmdHelper OleCmdHelper
        {
            get
            {
                if (Application.Current == null)
                {
                    return null;
                }

                return (OleCmdHelper) Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Send,
                (DispatcherOperationCallback) delegate(object unused)
                {
                    // V3.5: Check for Application object shutting down only.
                    // Consider to check for Browser shutting down.
                    if (Application.IsApplicationObjectShuttingDown == true)
                        return null;

                    if (_oleCmdHelper == null)
                    {
                        _oleCmdHelper = new OleCmdHelper();
                    }
                    return _oleCmdHelper;
                },
                null);
            }
        }

        internal static ApplicationProxyInternal Current
        {
            get { return _proxyInstance; }
        }

        internal Uri Uri
        {
            get
            {
                return _criticalUri.Value;
            }
            private set
            {
                _criticalUri.Value = value;

                // We need to set these properties now, because there are times during the application's lifetime
                // when the source URI would be useful, but the ApplicationProxyInternal has come and gone.
                // If there is DebugSecurityZoneUrl, SiteOfOriginContainer.BrowserSource would have been set to that value.
                // We don't want to overwrite that, so check whether the value is null or not before setting.
                if (SiteOfOriginContainer.BrowserSource == null)
                {
                    SiteOfOriginContainer.BrowserSource = value;
                }
            }
        }

        internal void SetDebugSecurityZoneURL(Uri debugSecurityZoneURL)
        {
            SiteOfOriginContainer.BrowserSource = debugSecurityZoneURL;
        }

        internal object StreamContainer
        {
            set
            {
                _unmanagedStream = new SecurityCriticalData<object>(Marshal.GetObjectForIUnknown((IntPtr)value));
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        private void OnStartup(Object sender, StartupEventArgs e)
        {
            //We listen to the Startup event only for history loads in which
            //case we want to do our journaling load instead of StartupUri load
            e.PerformDefaultAction = false;
            Application.Current.Startup -= new System.Windows.StartupEventHandler(this.OnStartup);
        }


        private void ClearRootBrowserWindow()
        {
            RootBrowserWindow = null;
        }

        private static void WriteInt32(Stream stream, int value)
        {
            stream.WriteByte((byte)((value & 0xFF000000) >> 24));
            stream.WriteByte((byte)((value & 0x00FF0000) >> 16));
            stream.WriteByte((byte)((value & 0x0000FF00) >> 8));
            stream.WriteByte((byte)((value & 0x000000FF)));
        }
        
        private static int ReadInt32(Stream stream)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                int b = stream.ReadByte();
                if (b < 0) 
                {
                    throw new EndOfStreamException();
                }
                
                value = (value << 8) | b;
            }
            
            return value;
        }
        
        #endregion Private Methods

        #region Private Properties

        private IServiceProvider ServiceProvider
        {
            set
            {
                _serviceProvider = value;

                if (Application.Current != null)
                {
                    Application.Current.ServiceProvider = value;
                }
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        // Instance fields.

        // These are fields that moved from DocobjHost class so we can have a
        // unified way of calling for single vs multiple AppDomain scenarios.

        private SecurityCriticalDataForSet<RootBrowserWindow>   _rbw;

        private SecurityCriticalDataForSet<RootBrowserWindowProxy> _rbwProxy;

        private bool                _show;
        private OleCmdHelper        _oleCmdHelper;

        // The following variables are used to remember the window size until the window is created
        // because the OLE SetRect/Show calls happen before the app and window are created internally
        Rect                        _windowRect;
        bool                        _rectset;

        private SecurityCriticalDataForSet<Uri> _criticalUri;
        SecurityCriticalDataClass<StorageRoot> _storageRoot = new SecurityCriticalDataClass<StorageRoot>(null);
        SecurityCriticalDataForSet<MimeType> _mimeType;
        IServiceProvider _serviceProvider ;

        private static ApplicationProxyInternal _proxyInstance;

        private const string FRAGMENT_MARKER = "#";
        
        private const byte JournalIdHeader = 0x01;
        private const byte BrowserJournalHeader = 0x02;

        #region XpsViewer (DocumentApplication) Specific
        /// <summary>
        /// This is an unmanaged COM IStream that is provided by the byte range downloader
        /// (progressive download) and comes from our unmanaged host.
        /// </summary>
        SecurityCriticalData<object> _unmanagedStream = new SecurityCriticalData<object>(null);

        /// <summary>
        /// This is a ByteWrapper a managed class that is an adapter from IStream to Stream.
        /// The stream it wraps is the _unmanagedStream.
        /// </summary>
        SecurityCriticalData<Stream> _packageStream = new SecurityCriticalData<Stream>(null);

        /// <summary>
        /// This contains many streams and packages that represent the current 'Package'
        /// for the XpsViewer.
        /// </summary>
        // The Document has been weakly-typed to avoid PresentationFramework
        // having a type dependency on PresentationUI.  The perf impact of the weak
        // typed variables in this case was determined to be much less than forcing the load
        // of a new assembly when Assembly.GetTypes was called on PresentationFramework.
        SecurityCriticalDataForSet<object> _document;
        #endregion

        #endregion Private Fields

        #region Private Structs
        /// <summary>
        /// Holder for all things to be persisted in the BrowserJournal before we
        /// navigate away from the app
        /// </summary>
        [Serializable]
        private struct BrowserJournal
        {
            #region Constructors
            internal BrowserJournal(Journal journal, Uri baseUri)
            {
                _journal = journal;
                _baseUri = baseUri;
            }
            #endregion Constructors

            #region Properties
            internal Journal Journal
            {
                get { return _journal; }
            }

            internal Uri BaseUri
            {
                get { return _baseUri; }
            }
            #endregion Properties

            #region Private Fields
            private Journal         _journal;
            private Uri             _baseUri;
            #endregion Private Fields
        }
        #endregion Private Structs

    }
}
